using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Redis;

public class RedisPresenceService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisPresenceService> _logger;
    private static readonly TimeSpan PresenceTtl = TimeSpan.FromMinutes(5);
    private const string OnlineTrackingSet = "presence:online";

    public RedisPresenceService(IConnectionMultiplexer redis, ILogger<RedisPresenceService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

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

    public async Task<string[]> GetOnlineUserIdsAsync()
    {
        var members = await _db.SetMembersAsync(OnlineTrackingSet);
        return members
            .Where(m => m.HasValue)
            .Select(m => m.ToString())
            .ToArray();
    }
}
