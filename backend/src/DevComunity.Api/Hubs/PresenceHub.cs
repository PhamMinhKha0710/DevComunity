using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace DevComunity.Api.Hubs;

/// <summary>
/// SignalR Hub for user presence/online status
/// </summary>
public class PresenceHub : Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> OnlineUsers = new();
    private readonly ILogger<PresenceHub> _logger;

    public PresenceHub(ILogger<PresenceHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            OnlineUsers.AddOrUpdate(
                userId,
                _ => new HashSet<string> { Context.ConnectionId },
                (_, connections) =>
                {
                    connections.Add(Context.ConnectionId);
                    return connections;
                });

            await Clients.Others.SendAsync("UserOnline", userId);
            _logger.LogInformation("User {UserId} is now online", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            if (OnlineUsers.TryGetValue(userId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                {
                    OnlineUsers.TryRemove(userId, out _);
                    await Clients.Others.SendAsync("UserOffline", userId);
                    _logger.LogInformation("User {UserId} is now offline", userId);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Get list of online users
    /// </summary>
    public Task<string[]> GetOnlineUsers()
    {
        return Task.FromResult(OnlineUsers.Keys.ToArray());
    }

    /// <summary>
    /// Check if a user is online
    /// </summary>
    public Task<bool> IsUserOnline(string userId)
    {
        return Task.FromResult(OnlineUsers.ContainsKey(userId));
    }
}
