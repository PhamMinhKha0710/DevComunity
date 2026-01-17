using Microsoft.AspNetCore.SignalR;

namespace DevComunity.Api.Hubs;

/// <summary>
/// SignalR Hub for activity feed updates
/// </summary>
public class ActivityHub : Hub
{
    private readonly ILogger<ActivityHub> _logger;

    public ActivityHub(ILogger<ActivityHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        // Join global activity feed
        await Groups.AddToGroupAsync(Context.ConnectionId, "activity_feed");
        _logger.LogInformation("Connection {ConnectionId} joined activity feed", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "activity_feed");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Broadcast new question activity
    /// </summary>
    public async Task BroadcastNewQuestion(object question)
    {
        await Clients.Group("activity_feed").SendAsync("NewQuestion", question);
    }

    /// <summary>
    /// Broadcast new answer activity
    /// </summary>
    public async Task BroadcastNewAnswer(object answer)
    {
        await Clients.Group("activity_feed").SendAsync("NewAnswer", answer);
    }

    /// <summary>
    /// Broadcast badge earned
    /// </summary>
    public async Task BroadcastBadgeEarned(object badgeInfo)
    {
        await Clients.Group("activity_feed").SendAsync("BadgeEarned", badgeInfo);
    }
}
