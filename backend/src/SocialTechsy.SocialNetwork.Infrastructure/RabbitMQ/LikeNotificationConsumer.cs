using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Infrastructure.RabbitMQ;

/// <summary>
/// Background worker that consumes like events from RabbitMQ and creates SQL notifications.
/// SignalR push is handled separately by the API layer via ILikeNotificationHandler.
/// </summary>
public class LikeNotificationConsumer : BackgroundService
{
    public const string QueueName = "social.events.likes";

    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LikeNotificationConsumer> _logger;
    private IChannel? _channel;

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
        _logger.LogInformation("LikeNotificationConsumer starting");

        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: SocialEventPublisher.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await _channel.QueueBindAsync(QueueName, SocialEventPublisher.ExchangeName, "like.*");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.Span);
                var evt = JsonSerializer.Deserialize<LikeEvent>(body);

                if (evt != null)
                    await ProcessLikeEventAsync(evt);

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process like event");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer);

        stoppingToken.Register(() =>
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessLikeEventAsync(LikeEvent evt)
    {
        if (evt.LikedByUserId == evt.ContentAuthorId) return;

        using var scope = _serviceProvider.CreateScope();
        var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

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

        // Dispatch SignalR push via the handler registered in the API layer
        var signalRHandler = scope.ServiceProvider.GetService<ILikeNotificationHandler>();
        if (signalRHandler != null)
        {
            await signalRHandler.HandleAsync(evt, notification);
        }

        _logger.LogInformation("Processed like notification: {TargetType}:{TargetId} by user {UserId}",
            evt.TargetType, evt.TargetId, evt.LikedByUserId);
    }
}
