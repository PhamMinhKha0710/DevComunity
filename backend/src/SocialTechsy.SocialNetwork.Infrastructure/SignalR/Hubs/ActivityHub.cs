using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SocialTechsy.SocialNetwork.Infrastructure.SignalR.Hubs;

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
        _logger.LogInformation("Connection {ConnectionId} left activity feed", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a group feed to receive group-specific post notifications
    /// </summary>
    public async Task JoinGroup(int groupId)
    {
        var groupName = $"group_{groupId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} joined group feed {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a group feed
    /// </summary>
    public async Task LeaveGroup(int groupId)
    {
        var groupName = $"group_{groupId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} left group feed {GroupName}", Context.ConnectionId, groupName);
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
