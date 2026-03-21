using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Notification;

namespace SocialTechsy.SocialNetwork.Application.Queries.Notifications;

/// <summary>
/// Query for getting user notifications
/// </summary>
public class GetNotificationsQuery : IRequest<NotificationsResponse>
{
    public int UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool UnreadOnly { get; set; } = false;
}

/// <summary>
/// Query for getting unread notification count
/// </summary>
public class GetUnreadCountQuery : IRequest<int>
{
    public int UserId { get; set; }
}
