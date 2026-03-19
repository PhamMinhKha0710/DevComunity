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

namespace SocialTechsy.SocialNetwork.Infrastructure.BackgroundJobs;

public class LikeNotificationConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LikeNotificationConsumer> _logger;
    private IChannel? _channel;
    private const string ExchangeName = "social.events";
    private const string QueueName = "like-notification-processor";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public LikeNotificationConsumer(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<LikeNotificationConsumer> logger)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(QueueName, ExchangeName, "like.new", cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    await ProcessLikeEventAsync(ea, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing like event");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
            _logger.LogInformation("LikeNotificationConsumer started, listening on queue {Queue}", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LikeNotificationConsumer failed to start");
        }
    }

    private async Task ProcessLikeEventAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());
        var likeEvent = JsonSerializer.Deserialize<LikeEvent>(body, JsonOptions);
        if (likeEvent == null) return;

        var command = new CreateLikeNotificationCommand
        {
            EventId = likeEvent.EventId,
            TargetType = likeEvent.TargetType,
            TargetId = likeEvent.TargetId,
            LikedByUserId = likeEvent.LikedByUserId,
            LikedByDisplayName = likeEvent.LikedByDisplayName,
            ContentAuthorId = likeEvent.ContentAuthorId,
            ContentTitle = likeEvent.ContentTitle ?? string.Empty,
            QuestionId = likeEvent.QuestionId ?? 0,
            LikeCount = likeEvent.LikeCount
        };

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(command, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
            await _channel.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
