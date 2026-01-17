namespace DevComunity.Application.Queries.Notifications;

/// <summary>
/// Query for getting user notifications
/// </summary>
public class GetNotificationsQuery
{
    public int UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool UnreadOnly { get; set; } = false;
}

/// <summary>
/// Query for getting unread notification count
/// </summary>
public class GetUnreadCountQuery
{
    public int UserId { get; set; }
}
