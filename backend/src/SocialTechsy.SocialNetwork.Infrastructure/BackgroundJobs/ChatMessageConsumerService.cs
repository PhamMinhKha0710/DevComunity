using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SocialTechsy.SocialNetwork.Application.Commands.Notifications;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.BackgroundJobs;

public class ChatMessageConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatMessageConsumerService> _logger;
    private readonly IConnectionMultiplexer? _redis;
    private IChannel? _channel;
    private const string ExchangeName = "chat.events";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ChatMessageConsumerService(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<ChatMessageConsumerService> logger,
        IConnectionMultiplexer? redis)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _redis = redis;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);

            var queueName = "chat-notification-processor";
            await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

            var routingKeys = new[]
            {
                ChatEventTypes.NewMessage,
                ChatEventTypes.MessagesRead,
                ChatEventTypes.ReactionAdded,
                ChatEventTypes.ReactionRemoved
            };

            foreach (var routingKey in routingKeys)
            {
                await _channel.QueueBindAsync(queueName, ExchangeName, routingKey, cancellationToken: stoppingToken);
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    await HandleEventAsync(ea, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing chat event");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
            _logger.LogInformation("ChatMessageConsumerService started, listening on queue {Queue}", queueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatMessageConsumerService failed to start");
        }
    }

    private async Task HandleEventAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var routingKey = ea.RoutingKey;
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());

        if (routingKey == ChatEventTypes.NewMessage)
        {
            var payload = JsonSerializer.Deserialize<NewMessagePayload>(body, JsonOptions);
            if (payload == null) return;

            var command = new CreateChatNotificationCommand
            {
                ConversationId = payload.ConversationId,
                MessageId = payload.MessageId,
                SenderId = payload.SenderId,
                SenderUsername = payload.SenderUsername,
                RecipientIds = payload.ParticipantUserIds.Where(id => id != payload.SenderId).ToList(),
                Timestamp = DateTime.UtcNow
            };

            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(command, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
            await _channel.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
