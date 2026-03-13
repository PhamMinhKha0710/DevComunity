using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.Redis;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.RabbitMQ;

public class ChatMessageConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatMessageConsumerService> _logger;
    private readonly IConnectionMultiplexer? _redis;
    private IChannel? _channel;

    public ChatMessageConsumerService(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<ChatMessageConsumerService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _redis = redis;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChatMessageConsumerService starting...");

        try
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: RabbitMqChatMessageBroker.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: RabbitMqChatMessageBroker.QueueName + ".dlq",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            var queueArgs = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = RabbitMqChatMessageBroker.QueueName + ".dlq"
            };

            try
            {
                await _channel.QueueDeclareAsync(
                    queue: RabbitMqChatMessageBroker.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: queueArgs,
                    cancellationToken: stoppingToken);
            }
            catch (OperationInterruptedException ex)
                when (ex.Message.Contains("PRECONDITION_FAILED"))
            {
                _logger.LogWarning("Queue {Queue} has incompatible args, recreating with DLQ support...",
                    RabbitMqChatMessageBroker.QueueName);

                // Channel is broken after PRECONDITION_FAILED, create a new one
                _channel.Dispose();
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await _channel.QueueDeleteAsync(
                    queue: RabbitMqChatMessageBroker.QueueName,
                    cancellationToken: stoppingToken);

                await _channel.QueueDeclareAsync(
                    queue: RabbitMqChatMessageBroker.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: queueArgs,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("Queue {Queue} recreated with DLQ support",
                    RabbitMqChatMessageBroker.QueueName);
            }

            // Bind all chat event types
            await _channel.QueueBindAsync(
                queue: RabbitMqChatMessageBroker.QueueName,
                exchange: RabbitMqChatMessageBroker.ExchangeName,
                routingKey: "message.#",
                cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(
                queue: RabbitMqChatMessageBroker.QueueName,
                exchange: RabbitMqChatMessageBroker.ExchangeName,
                routingKey: "reaction.#",
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var chatEvent = JsonSerializer.Deserialize<ChatEvent>(json);

                    if (chatEvent != null)
                    {
                        await ProcessEventAsync(chatEvent);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing chat event");
                    var retryCount = GetRetryCount(ea.BasicProperties);
                    if (retryCount >= 3)
                    {
                        _logger.LogWarning("Message exceeded max retries ({RetryCount}), moving to DLQ", retryCount);
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                    else
                    {
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                }
            };

            await _channel.BasicConsumeAsync(
                queue: RabbitMqChatMessageBroker.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("ChatMessageConsumerService started, listening for events");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("ChatMessageConsumerService stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatMessageConsumerService encountered an error");
        }
    }

    private async Task ProcessEventAsync(ChatEvent chatEvent)
    {
        // Idempotency check via Redis
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var idempotencyKey = $"evt:processed:{chatEvent.EventId}";
            var wasSet = await db.StringSetAsync(idempotencyKey, "1", TimeSpan.FromHours(24), StackExchange.Redis.When.NotExists);
            if (!wasSet)
            {
                _logger.LogDebug("Skipping duplicate event {Type} at {Timestamp}", chatEvent.Type, chatEvent.Timestamp);
                return;
            }
        }

        switch (chatEvent.Type)
        {
            case ChatEventTypes.NewMessage:
                await HandleNewMessageAsync(chatEvent);
                break;
            case ChatEventTypes.MessagesRead:
                await HandleMessagesReadAsync(chatEvent);
                break;
            case ChatEventTypes.ReactionAdded:
            case ChatEventTypes.ReactionRemoved:
                _logger.LogDebug("Processed reaction event: {Type}", chatEvent.Type);
                break;
            default:
                _logger.LogWarning("Unknown chat event type: {Type}", chatEvent.Type);
                break;
        }
    }

    private async Task HandleNewMessageAsync(ChatEvent chatEvent)
    {
        var payload = JsonSerializer.Deserialize<NewMessagePayload>(chatEvent.PayloadJson);
        if (payload == null) return;

        using var scope = _serviceProvider.CreateScope();
        var notificationRepo = scope.ServiceProvider.GetService<INotificationRepository>();
        var chatPushHandler = scope.ServiceProvider.GetService<IChatPushHandler>();
        var presenceService = scope.ServiceProvider.GetService<RedisPresenceService>();

        var recipientIds = payload.ParticipantUserIds.Where(id => id != payload.SenderId).ToList();

        if (notificationRepo != null)
        {
            foreach (var participantId in recipientIds)
            {
                var notification = new Domain.Entities.Notification
                {
                    UserId = participantId,
                    FromUserId = payload.SenderId,
                    Type = "chat_message",
                    Message = $"New message from {payload.SenderUsername}",
                    Link = $"/chat?conversation={payload.ConversationId}",
                    IsRead = false,
                    CreatedDate = chatEvent.Timestamp
                };
                await notificationRepo.AddAsync(notification);
            }
        }

        if (chatPushHandler != null)
        {
            var pushEvent = new ChatPushEvent
            {
                EventId = chatEvent.EventId,
                ConversationId = payload.ConversationId,
                MessageId = payload.MessageId,
                SenderId = payload.SenderId,
                SenderUsername = payload.SenderUsername,
                NotificationPreview = $"New message from {payload.SenderUsername}",
                RecipientUserIds = recipientIds
            };

            await chatPushHandler.PushNewMessageNotificationAsync(pushEvent);

            if (presenceService != null)
            {
                foreach (var uid in recipientIds)
                {
                    var isOnline = await presenceService.IsOnlineAsync(uid.ToString());
                    if (!isOnline)
                    {
                        var pendingPayload = JsonSerializer.Serialize(new
                        {
                            type = "chat_message",
                            payload.ConversationId,
                            payload.MessageId,
                            payload.SenderUsername
                        });
                        await presenceService.AddPendingPushAsync(uid.ToString(), pendingPayload);
                    }
                }
            }
        }

        _logger.LogDebug("Processed new message event for conversation {ConversationId}", payload.ConversationId);
    }

    private async Task HandleMessagesReadAsync(ChatEvent chatEvent)
    {
        var payload = JsonSerializer.Deserialize<MessagesReadPayload>(chatEvent.PayloadJson);
        if (payload == null) return;

        _logger.LogDebug("Processed messages read event: user {UserId} read conversation {ConversationId}",
            payload.UserId, payload.ConversationId);

        await Task.CompletedTask;
    }

    private static int GetRetryCount(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers != null &&
            properties.Headers.TryGetValue("x-death", out var deathObj) &&
            deathObj is List<object> deathList && deathList.Count > 0 &&
            deathList[0] is Dictionary<string, object> death &&
            death.TryGetValue("count", out var countObj))
        {
            return Convert.ToInt32(countObj);
        }
        return 0;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }
        await base.StopAsync(cancellationToken);
    }
}
