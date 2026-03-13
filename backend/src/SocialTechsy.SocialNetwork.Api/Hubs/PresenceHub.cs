using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Infrastructure.Redis;

namespace SocialTechsy.SocialNetwork.Api.Hubs;

[Authorize]
public class PresenceHub : Hub
{
    private readonly ILogger<PresenceHub> _logger;
    private readonly RedisPresenceService? _presence;
    private readonly IFriendshipRepository _friendshipRepository;

    public PresenceHub(
        ILogger<PresenceHub> logger,
        IFriendshipRepository friendshipRepository,
        RedisPresenceService? presence = null)
    {
        _logger = logger;
        _friendshipRepository = friendshipRepository;
        _presence = presence;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out var uid))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            var friendIds = await EnsureFriendsCachedAsync(userId, uid);
            foreach (var fid in friendIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"friends_of_{fid}");

            if (_presence != null)
            {
                var wasOffline = !await _presence.IsOnlineAsync(userId);
                await _presence.SetOnlineAsync(userId, Context.ConnectionId);
                if (wasOffline)
                {
                    await Clients.Group($"friends_of_{userId}").SendAsync("UserOnline", userId);
                    await DrainPendingPushAsync(userId);
                }
            }
            else
            {
                await Clients.Group($"friends_of_{userId}").SendAsync("UserOnline", userId);
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
                    await Clients.Group($"friends_of_{userId}").SendAsync("UserOffline", userId);
                    _logger.LogInformation("User {UserId} is now offline", userId);
                }
            }
            else
            {
                await Clients.Group($"friends_of_{userId}").SendAsync("UserOffline", userId);
                _logger.LogInformation("User {UserId} is now offline", userId);
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
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

    public async Task<string[]> GetOnlineFriends()
    {
        var userId = Context.UserIdentifier;
        if (_presence != null && !string.IsNullOrEmpty(userId))
        {
            return await _presence.GetOnlineFriendsAsync(userId);
        }
        return Array.Empty<string>();
    }

    [Obsolete("Use GetOnlineFriends for scoped presence")]
    public async Task<string[]> GetOnlineUsers()
    {
        if (_presence != null)
        {
#pragma warning disable CS0618
            return await _presence.GetOnlineUserIdsAsync();
#pragma warning restore CS0618
        }
        return Array.Empty<string>();
    }

    public async Task<string[]> GetOnlineInConversation(int conversationId)
    {
        if (_presence == null) return Array.Empty<string>();

        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out _))
            return Array.Empty<string>();

        var chatRepo = Context.GetHttpContext()?.RequestServices
            .GetService<Application.Interfaces.Repositories.IChatRepository>();
        if (chatRepo == null) return Array.Empty<string>();

        var conversation = await chatRepo.GetConversationByIdAsync(conversationId);
        if (conversation == null) return Array.Empty<string>();

        var participantIds = conversation.Participants
            .Select(p => p.UserId.ToString())
            .Where(id => id != userId);

        return await _presence.FilterOnlineAsync(participantIds);
    }

    public async Task<bool> IsUserOnline(string userId)
    {
        if (_presence != null)
        {
            return await _presence.IsOnlineAsync(userId);
        }
        return false;
    }

    private async Task DrainPendingPushAsync(string userId)
    {
        if (_presence == null) return;

        try
        {
            var pendingItems = await _presence.DrainPendingPushAsync(userId);
            if (pendingItems.Length == 0) return;

            await Clients.Caller.SendAsync("PendingNotifications", new
            {
                count = pendingItems.Length,
                items = pendingItems
            });

            _logger.LogInformation("Drained {Count} pending push items for user {UserId}",
                pendingItems.Length, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to drain pending push for user {UserId}", userId);
        }
    }

    private async Task<string[]> EnsureFriendsCachedAsync(string userId, int uid)
    {
        if (_presence != null && await _presence.HasCachedFriendsAsync(userId))
            return await _presence.GetCachedFriendIdsAsync(userId);

        var friendships = await _friendshipRepository.GetFriendsAsync(uid);
        var friendIds = friendships
            .Select(f => f.RequesterId == uid ? f.AddresseeId : f.RequesterId)
            .ToList();

        if (_presence != null)
            await _presence.CacheFriendIdsAsync(userId, friendIds);

        return friendIds.Select(id => id.ToString()).ToArray();
    }
}
