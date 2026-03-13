using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Redis;

public class RedisPresenceService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisPresenceService> _logger;
    private static readonly TimeSpan PresenceTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FriendsCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan PendingPushTtl = TimeSpan.FromDays(7);
    private const string OnlineTrackingSet = "presence:online";

    public RedisPresenceService(IConnectionMultiplexer redis, ILogger<RedisPresenceService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    // ========== CONNECTION STATE ==========

    public async Task SetOnlineAsync(string userId, string connectionId)
    {
        var key = $"presence:user:{userId}";
        await _db.SetAddAsync(key, connectionId);
        await _db.KeyExpireAsync(key, PresenceTtl);
        await _db.SetAddAsync(OnlineTrackingSet, userId);
    }

    public async Task SetOfflineAsync(string userId, string connectionId)
    {
        var key = $"presence:user:{userId}";
        await _db.SetRemoveAsync(key, connectionId);
        var remaining = await _db.SetLengthAsync(key);
        if (remaining == 0)
        {
            await _db.KeyDeleteAsync(key);
            await _db.SetRemoveAsync(OnlineTrackingSet, userId);
        }
    }

    public async Task<bool> IsOnlineAsync(string userId)
    {
        return await _db.KeyExistsAsync($"presence:user:{userId}");
    }

    public async Task<bool> HasConnectionsAsync(string userId)
    {
        return await _db.SetLengthAsync($"presence:user:{userId}") > 0;
    }

    public async Task HeartbeatAsync(string userId)
    {
        var key = $"presence:user:{userId}";
        if (await _db.KeyExistsAsync(key))
        {
            await _db.KeyExpireAsync(key, PresenceTtl);
        }
    }

    // ========== FRIEND-SCOPED PRESENCE ==========

    public async Task CacheFriendIdsAsync(string userId, IEnumerable<int> friendIds)
    {
        var key = $"presence:friends:{userId}";
        await _db.KeyDeleteAsync(key);
        var values = friendIds.Select(id => (RedisValue)id.ToString()).ToArray();
        if (values.Length > 0)
        {
            await _db.SetAddAsync(key, values);
        }
        await _db.KeyExpireAsync(key, FriendsCacheTtl);
    }

    public async Task<string[]> GetCachedFriendIdsAsync(string userId)
    {
        var members = await _db.SetMembersAsync($"presence:friends:{userId}");
        return members.Where(m => m.HasValue).Select(m => m.ToString()).ToArray();
    }

    public async Task<bool> HasCachedFriendsAsync(string userId)
    {
        return await _db.KeyExistsAsync($"presence:friends:{userId}");
    }

    public async Task<string[]> GetOnlineFriendsAsync(string userId)
    {
        var friendIds = await GetCachedFriendIdsAsync(userId);
        if (friendIds.Length == 0) return Array.Empty<string>();

        var tasks = friendIds.Select(id => IsOnlineAsync(id));
        var results = await Task.WhenAll(tasks);

        return friendIds.Where((_, i) => results[i]).ToArray();
    }

    public async Task<string[]> FilterOnlineAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.ToArray();
        if (ids.Length == 0) return Array.Empty<string>();

        var tasks = ids.Select(id => IsOnlineAsync(id));
        var results = await Task.WhenAll(tasks);
        return ids.Where((_, i) => results[i]).ToArray();
    }

    [Obsolete("Use GetOnlineFriendsAsync for scoped presence")]
    public async Task<string[]> GetOnlineUserIdsAsync()
    {
        var members = await _db.SetMembersAsync(OnlineTrackingSet);
        return members
            .Where(m => m.HasValue)
            .Select(m => m.ToString())
            .ToArray();
    }

    // ========== PENDING PUSH (offline message queue) ==========

    public async Task AddPendingPushAsync(string userId, string payload)
    {
        var key = $"presence:pending_push:{userId}";
        await _db.ListRightPushAsync(key, payload);
        await _db.KeyExpireAsync(key, PendingPushTtl);
    }

    public async Task<string[]> DrainPendingPushAsync(string userId, int maxItems = 100)
    {
        var key = $"presence:pending_push:{userId}";
        var items = new List<string>();
        for (var i = 0; i < maxItems; i++)
        {
            var item = await _db.ListLeftPopAsync(key);
            if (!item.HasValue) break;
            items.Add(item.ToString());
        }
        return items.ToArray();
    }

    public async Task<long> GetPendingPushCountAsync(string userId)
    {
        return await _db.ListLengthAsync($"presence:pending_push:{userId}");
    }
}
