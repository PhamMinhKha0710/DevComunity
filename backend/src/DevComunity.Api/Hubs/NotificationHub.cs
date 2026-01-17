using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DevComunity.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
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

    /// <summary>
    /// Send notification to a specific user
    /// </summary>
    public async Task SendNotification(string userId, object notification)
    {
        await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification);
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public async Task MarkAsRead(int notificationId)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} marked notification {NotificationId} as read", userId, notificationId);

        // TODO: Update in database
        await Clients.Caller.SendAsync("NotificationRead", notificationId);
    }
}
