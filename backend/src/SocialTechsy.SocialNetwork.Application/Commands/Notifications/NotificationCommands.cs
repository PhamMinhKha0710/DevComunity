using MediatR;

namespace SocialTechsy.SocialNetwork.Application.Commands.Notifications;

/// <summary>
/// Command to mark a notification as read
/// </summary>
public class MarkNotificationReadCommand : IRequest<bool>
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command to mark all notifications as read
/// </summary>
public class MarkAllNotificationsReadCommand : IRequest
{
    public int UserId { get; set; }
}

/// <summary>
/// Command to delete a notification
/// </summary>
public class DeleteNotificationCommand : IRequest<bool>
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
}
