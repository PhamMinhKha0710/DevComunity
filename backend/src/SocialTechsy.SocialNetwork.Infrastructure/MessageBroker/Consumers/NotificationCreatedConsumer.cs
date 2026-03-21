using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Application.MessageBroker.Events;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Infrastructure.MessageBroker.Consumers;

public class NotificationCreatedConsumer : IConsumer<NotificationCreatedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationCreatedConsumer> _logger;

    public NotificationCreatedConsumer(
        IServiceProvider serviceProvider,
        ILogger<NotificationCreatedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<NotificationCreatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "NotificationCreatedConsumer: Received NotificationCreatedEvent. ReceiverId={ReceiverId}, ActorId={ActorId}, Type={Type}",
            evt.ReceiverId, evt.ActorId, evt.Type);

        using var scope = _serviceProvider.CreateScope();
        var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        var notification = new Notification
        {
            UserId = evt.ReceiverId,
            FromUserId = evt.ActorId,
            Type = evt.Type,
            Message = evt.Message,
            Link = evt.Link,
            CreatedDate = evt.CreatedAt,
            IsRead = false
        };

        await notificationRepo.AddAsync(notification, context.CancellationToken);

        // Notify via SignalR NotificationHub utilizing generic dispatcher
        var dispatcher = scope.ServiceProvider.GetService<INotificationDispatcher>();
        if (dispatcher != null)
        {
            await dispatcher.DispatchAsync(notification, context.CancellationToken);
        }
        else
        {
            _logger.LogWarning("INotificationDispatcher not found in DI. SignalR notification won't be sent.");
        }
    }
}
