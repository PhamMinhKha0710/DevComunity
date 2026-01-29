using DevComunity.Application.Queries.Notifications;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;

namespace DevComunity.Application.QueryHandlers.Notifications;

/// <summary>
/// Handler for getting user notifications
/// </summary>
public class GetNotificationsQueryHandler
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<NotificationsResponse> HandleAsync(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _notificationRepository.GetByUserIdAsync(
            query.UserId,
            query.Page,
            query.PageSize,
            query.UnreadOnly,
            cancellationToken);

        var unreadCount = await _notificationRepository.GetUnreadCountAsync(query.UserId, cancellationToken);

        return new NotificationsResponse
        {
            Items = items.Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedDate = n.CreatedDate,
                Link = n.Link
            }).ToList(),
            UnreadCount = unreadCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Handler for getting unread notification count
/// </summary>
public class GetUnreadCountQueryHandler
{
    private readonly INotificationRepository _notificationRepository;

    public GetUnreadCountQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<int> HandleAsync(GetUnreadCountQuery query, CancellationToken cancellationToken)
    {
        return await _notificationRepository.GetUnreadCountAsync(query.UserId, cancellationToken);
    }
}
