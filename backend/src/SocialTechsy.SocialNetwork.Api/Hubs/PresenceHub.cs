using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Infrastructure.Redis;

namespace SocialTechsy.SocialNetwork.Api.Hubs;

[Authorize]
public class PresenceHub : Hub
{
    private readonly ILogger<PresenceHub> _logger;
    private readonly RedisPresenceService? _presence;

    public PresenceHub(ILogger<PresenceHub> logger, RedisPresenceService? presence = null)
    {
        _logger = logger;
        _presence = presence;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            if (_presence != null)
            {
                var wasOffline = !await _presence.IsOnlineAsync(userId);
                await _presence.SetOnlineAsync(userId, Context.ConnectionId);
                if (wasOffline)
                {
                    await Clients.Others.SendAsync("UserOnline", userId);
                }
            }
            else
            {
                await Clients.Others.SendAsync("UserOnline", userId);
            }
            _logger.LogInformation("User {UserId} is now online", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            if (_presence != null)
            {
                await _presence.SetOfflineAsync(userId, Context.ConnectionId);
                var stillOnline = await _presence.HasConnectionsAsync(userId);
                if (!stillOnline)
                {
                    await Clients.Others.SendAsync("UserOffline", userId);
                    _logger.LogInformation("User {UserId} is now offline", userId);
                }
            }
            else
            {
                await Clients.Others.SendAsync("UserOffline", userId);
                _logger.LogInformation("User {UserId} is now offline", userId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task Heartbeat()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId) && _presence != null)
        {
            await _presence.HeartbeatAsync(userId);
        }
    }

    public async Task<string[]> GetOnlineUsers()
    {
        if (_presence != null)
        {
            return await _presence.GetOnlineUserIdsAsync();
        }
        return Array.Empty<string>();
    }

    public async Task<bool> IsUserOnline(string userId)
    {
        if (_presence != null)
        {
            return await _presence.IsOnlineAsync(userId);
        }
        return false;
    }
}
