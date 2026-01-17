namespace DevComunity.Application.Common.DTOs;

/// <summary>
/// DTO for notification
/// </summary>
public class NotificationDto
{
    public int NotificationId { get; set; }
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? Link { get; set; }
}

/// <summary>
/// Response for notifications with pagination and unread count
/// </summary>
public class NotificationsResponse
{
    public List<NotificationDto> Items { get; set; } = new();
    public int UnreadCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
