using SocialTechsy.SocialNetwork.Application.Commands.Notifications;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Notifications;

/// <summary>
/// Handler for marking a notification as read
/// </summary>
public class MarkNotificationReadCommandHandler
{
    private readonly INotificationRepository _notificationRepository;

    public MarkNotificationReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<bool> HandleAsync(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(command.NotificationId, cancellationToken);
        if (notification == null || notification.UserId != command.UserId)
            return false;

        await _notificationRepository.MarkAsReadAsync(command.NotificationId, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for marking all notifications as read
/// </summary>
public class MarkAllNotificationsReadCommandHandler
{
    private readonly INotificationRepository _notificationRepository;

    public MarkAllNotificationsReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task HandleAsync(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        await _notificationRepository.MarkAllAsReadAsync(command.UserId, cancellationToken);
    }
}

/// <summary>
/// Handler for deleting a notification
/// </summary>
public class DeleteNotificationCommandHandler
{
    private readonly INotificationRepository _notificationRepository;

    public DeleteNotificationCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<bool> HandleAsync(DeleteNotificationCommand command, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(command.NotificationId, cancellationToken);
        if (notification == null || notification.UserId != command.UserId)
            return false;

        await _notificationRepository.DeleteAsync(command.NotificationId, cancellationToken);
        return true;
    }
}
