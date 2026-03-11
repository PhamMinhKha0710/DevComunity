using MediatR;
using SocialTechsy.SocialNetwork.Application.Queries.Notifications;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Notifications;

/// <summary>
/// Handler for getting user notifications
/// </summary>
public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, NotificationsResponse>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<NotificationsResponse> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _notificationRepository.GetByUserIdAsync(
            request.UserId,
            request.Page,
            request.PageSize,
            request.UnreadOnly,
            cancellationToken);

        var unreadCount = await _notificationRepository.GetUnreadCountAsync(request.UserId, cancellationToken);

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
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Handler for getting unread notification count
/// </summary>
public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly INotificationRepository _notificationRepository;

    public GetUnreadCountQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        return await _notificationRepository.GetUnreadCountAsync(request.UserId, cancellationToken);
    }
}
