using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    private readonly INotificationRepository _notificationRepository;

    public NotificationHub(ILogger<NotificationHub> logger, INotificationRepository notificationRepository)
    {
        _logger = logger;
        _notificationRepository = notificationRepository;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to NotificationHub", userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendNotification(string userId, object notification)
    {
        await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification);
    }

    public async Task MarkAsRead(int notificationId)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} marked notification {NotificationId} as read", userId, notificationId);

        await _notificationRepository.MarkAsReadAsync(notificationId);
        await Clients.Caller.SendAsync("NotificationRead", notificationId);
    }

    public async Task MarkAllAsRead()
    {
        var userId = Context.UserIdentifier;
        if (userId != null && int.TryParse(userId, out var uid))
        {
            _logger.LogInformation("User {UserId} marked all notifications as read", userId);

            await _notificationRepository.MarkAllAsReadAsync(uid);
            await Clients.Caller.SendAsync("AllNotificationsRead");
        }
    }
}
