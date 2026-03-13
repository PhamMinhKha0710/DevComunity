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
using SocialTechsy.SocialNetwork.Domain.Entities;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.RabbitMQ;

/// <summary>
/// Background worker that consumes like events from RabbitMQ and creates SQL notifications.
/// Supports DLQ for failed messages, Redis-based idempotency, and retry with backoff.
/// </summary>
public class LikeNotificationConsumer : BackgroundService
{
    public const string QueueName = "social.events.likes";
    public const string DlqName = "social.events.likes.dlq";
    private const int MaxRetries = 3;

    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LikeNotificationConsumer> _logger;
    private readonly IConnectionMultiplexer? _redis;
    private IChannel? _channel;

    public LikeNotificationConsumer(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<LikeNotificationConsumer> logger,
        IConnectionMultiplexer? redis = null)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _redis = redis;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LikeNotificationConsumer starting");

        try
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: SocialEventPublisher.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: DlqName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            var queueArgs = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = DlqName
            };

            try
            {
                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: queueArgs,
                    cancellationToken: stoppingToken);
            }
            catch (OperationInterruptedException ex)
                when (ex.Message.Contains("PRECONDITION_FAILED"))
            {
                _logger.LogWarning("Queue {Queue} has incompatible args, recreating with DLQ support...", QueueName);
                _channel.Dispose();
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await _channel.QueueDeleteAsync(queue: QueueName, cancellationToken: stoppingToken);
                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: queueArgs,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("Queue {Queue} recreated with DLQ support", QueueName);
            }

            await _channel.QueueBindAsync(QueueName, SocialEventPublisher.ExchangeName, "like.*",
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = Encoding.UTF8.GetString(ea.Body.Span);
                    var evt = JsonSerializer.Deserialize<LikeEvent>(body);

                    if (evt != null)
                    {
                        if (await IsAlreadyProcessedAsync(evt.EventId))
                        {
                            _logger.LogDebug("Skipping duplicate like event {EventId}", evt.EventId);
                            await _channel.BasicAckAsync(ea.DeliveryTag, false);
                            return;
                        }

                        await ProcessLikeEventAsync(evt);
                        await MarkAsProcessedAsync(evt.EventId);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process like event");
                    var retryCount = GetRetryCount(ea.BasicProperties);
                    if (retryCount >= MaxRetries)
                    {
                        _logger.LogWarning("Like event exceeded max retries ({RetryCount}), moving to DLQ", retryCount);
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                    }
                    else
                    {
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
                    }
                }
            };

            await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("LikeNotificationConsumer started with DLQ and idempotency support");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("LikeNotificationConsumer stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LikeNotificationConsumer encountered an error");
        }
    }

    private async Task<bool> IsAlreadyProcessedAsync(string eventId)
    {
        if (_redis == null || string.IsNullOrEmpty(eventId)) return false;
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync($"like:evt:processed:{eventId}");
    }

    private async Task MarkAsProcessedAsync(string eventId)
    {
        if (_redis == null || string.IsNullOrEmpty(eventId)) return;
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"like:evt:processed:{eventId}", "1", TimeSpan.FromHours(24));
    }

    private async Task ProcessLikeEventAsync(LikeEvent evt)
    {
        if (evt.LikedByUserId == evt.ContentAuthorId) return;

        using var scope = _serviceProvider.CreateScope();
        var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        // Reconcile Redis like counter (idempotent SET add)
        var likeService = scope.ServiceProvider.GetService<ILikeService>();
        if (likeService != null)
        {
            await likeService.LikeAsync(evt.TargetType, evt.TargetId, evt.LikedByUserId);
        }

        var link = evt.TargetType == "question"
            ? $"/questions/{evt.TargetId}"
            : $"/questions/{evt.QuestionId}#answer-{evt.TargetId}";

        var message = evt.TargetType == "question"
            ? $"{evt.LikedByDisplayName} đã thích câu hỏi của bạn: {evt.ContentTitle}"
            : $"{evt.LikedByDisplayName} đã thích câu trả lời của bạn";

        var notification = new Notification
        {
            UserId = evt.ContentAuthorId,
            FromUserId = evt.LikedByUserId,
            Type = "Like",
            Message = message,
            Link = link,
            CreatedDate = DateTime.UtcNow,
            IsRead = false
        };

        await notificationRepo.AddAsync(notification, default);

        var signalRHandler = scope.ServiceProvider.GetService<ILikeNotificationHandler>();
        if (signalRHandler != null)
        {
            await signalRHandler.HandleAsync(evt, notification);
        }

        _logger.LogInformation("Processed like notification: {TargetType}:{TargetId} by user {UserId}",
            evt.TargetType, evt.TargetId, evt.LikedByUserId);
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
