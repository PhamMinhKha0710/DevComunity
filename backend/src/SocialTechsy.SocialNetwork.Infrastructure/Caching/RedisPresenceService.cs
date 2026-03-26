using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public class RedisPresenceService
{
    private readonly IDatabase? _db;
    private readonly ILogger<RedisPresenceService> _logger;
    private static readonly TimeSpan ConnectionTtl = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan FriendsCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan PendingPushTtl = TimeSpan.FromDays(7);
    private static readonly TimeSpan LastSeenTtl = TimeSpan.FromDays(30);

    public RedisPresenceService(IConnectionMultiplexer redis, ILogger<RedisPresenceService> logger)
    {
        _logger = logger;
        if (redis == null || !redis.IsConnected)
        {
            _logger.LogWarning("Redis unavailable - presence features disabled");
            return;
        }
        try { _db = redis.GetDatabase(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Redis - presence features disabled");
        }
    }

    private IDatabase GetDb() => _db!;
    private bool IsAvailable => _db != null;

    // ========== CONNECTION STATE (per-user TTL keys, no global SET) ==========

    public async Task SetOnlineAsync(string userId, string connectionId)
    {
        if (!IsAvailable) return;
        var key = $"presence:user:{userId}";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        await GetDb().SortedSetAddAsync(key, connectionId, double.Parse(ts));
        await GetDb().KeyExpireAsync(key, ConnectionTtl);
    }

    public async Task SetOfflineAsync(string userId, string connectionId)
    {
        if (!IsAvailable) return;
        var key = $"presence:user:{userId}";
        await GetDb().SortedSetRemoveAsync(key, connectionId);
        var remaining = await GetDb().SortedSetLengthAsync(key);
        if (remaining == 0)
        {
            await GetDb().KeyDeleteAsync(key);
            await GetDb().StringSetAsync($"presence:lastseen:{userId}",
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), LastSeenTtl);
        }
    }

    public async Task<bool> IsOnlineAsync(string userId)
    {
        if (!IsAvailable) return false;
        return await GetDb().KeyExistsAsync($"presence:user:{userId}");
    }

    public async Task<bool> HasConnectionsAsync(string userId)
    {
        if (!IsAvailable) return false;
        return await GetDb().SortedSetLengthAsync($"presence:user:{userId}") > 0;
    }

    public async Task HeartbeatAsync(string userId)
    {
        if (!IsAvailable) return;
        var key = $"presence:user:{userId}";
        if (await GetDb().KeyExistsAsync(key))
        {
            await GetDb().KeyExpireAsync(key, ConnectionTtl);
        }
    }

    public async Task<DateTimeOffset?> GetLastSeenAsync(string userId)
    {
        if (!IsAvailable) return null;
        var val = await GetDb().StringGetAsync($"presence:lastseen:{userId}");
        if (!val.HasValue) return null;
        return DateTimeOffset.FromUnixTimeSeconds(long.Parse(val!));
    }

    public async Task CacheFriendIdsAsync(string userId, IEnumerable<int> friendIds)
    {
        if (!IsAvailable) return;
        var key = $"presence:friends:{userId}";
        await GetDb().KeyDeleteAsync(key);
        var values = friendIds.Select(id => (RedisValue)id.ToString()).ToArray();
        if (values.Length > 0)
        {
            await GetDb().SetAddAsync(key, values);
        }
        await GetDb().KeyExpireAsync(key, FriendsCacheTtl);
    }

    public async Task<string[]> GetCachedFriendIdsAsync(string userId)
    {
        if (!IsAvailable) return Array.Empty<string>();
        var members = await GetDb().SetMembersAsync($"presence:friends:{userId}");
        return members.Where(m => m.HasValue).Select(m => m.ToString()).ToArray();
    }

    public async Task<bool> HasCachedFriendsAsync(string userId)
    {
        if (!IsAvailable) return false;
        return await GetDb().KeyExistsAsync($"presence:friends:{userId}");
    }

    public async Task<string[]> GetOnlineFriendsAsync(string userId)
    {
        if (!IsAvailable) return Array.Empty<string>();
        var friendIds = await GetCachedFriendIdsAsync(userId);
        if (friendIds.Length == 0) return Array.Empty<string>();

        var batch = GetDb().CreateBatch();
        var tasks = friendIds.Select(id =>
            batch.KeyExistsAsync($"presence:user:{id}")).ToArray();
        batch.Execute();
        var results = await Task.WhenAll(tasks);

        return friendIds.Where((_, i) => results[i]).ToArray();
    }

    public async Task<string[]> FilterOnlineAsync(IEnumerable<string> userIds)
    {
        if (!IsAvailable) return Array.Empty<string>();
        var ids = userIds.ToArray();
        if (ids.Length == 0) return Array.Empty<string>();

        var batch = GetDb().CreateBatch();
        var tasks = ids.Select(id =>
            batch.KeyExistsAsync($"presence:user:{id}")).ToArray();
        batch.Execute();
        var results = await Task.WhenAll(tasks);
        return ids.Where((_, i) => results[i]).ToArray();
    }

    public async Task AddPendingPushAsync(string userId, string payload)
    {
        if (!IsAvailable) return;
        var key = $"presence:pending_push:{userId}";
        await GetDb().ListRightPushAsync(key, payload);
        await GetDb().KeyExpireAsync(key, PendingPushTtl);
    }

    public async Task<string[]> DrainPendingPushAsync(string userId, int maxItems = 100)
    {
        if (!IsAvailable) return Array.Empty<string>();
        var key = $"presence:pending_push:{userId}";
        var items = new List<string>();
        for (var i = 0; i < maxItems; i++)
        {
            var item = await GetDb().ListLeftPopAsync(key);
            if (!item.HasValue) break;
            items.Add(item.ToString());
        }
        return items.ToArray();
    }

    public async Task<long> GetPendingPushCountAsync(string userId)
    {
        if (!IsAvailable) return 0;
        return await GetDb().ListLengthAsync($"presence:pending_push:{userId}");
    }

    [Obsolete("Use GetOnlineFriendsAsync for scoped presence")]
    public Task<string[]> GetOnlineUserIdsAsync()
    {
        return Task.FromResult(Array.Empty<string>());
    }
}
