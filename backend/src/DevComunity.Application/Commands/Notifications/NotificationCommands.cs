namespace DevComunity.Application.Commands.Notifications;

/// <summary>
/// Command to mark a notification as read
/// </summary>
public class MarkNotificationReadCommand
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command to mark all notifications as read
/// </summary>
public class MarkAllNotificationsReadCommand
{
    public int UserId { get; set; }
}

/// <summary>
/// Command to delete a notification
/// </summary>
public class DeleteNotificationCommand
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
}
