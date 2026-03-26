using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public class RedisChatRateLimiter
{
    private readonly IDatabase? _db;
    private readonly ILogger<RedisChatRateLimiter> _logger;

    public RedisChatRateLimiter(IConnectionMultiplexer redis, ILogger<RedisChatRateLimiter> logger)
    {
        _logger = logger;
        if (redis == null || !redis.IsConnected)
        {
            _logger.LogWarning("Redis unavailable - chat rate limiting disabled");
            return;
        }
        try { _db = redis.GetDatabase(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Redis - chat rate limiting disabled");
        }
    }

    private IDatabase GetDb() => _db!;
    private bool IsAvailable => _db != null;

    /// <summary>
    /// Sliding window rate limiter using Redis sorted sets.
    /// Entries auto-expire so there's no memory leak for inactive users.
    /// Works across multiple API instances.
    /// </summary>
    public async Task<bool> IsAllowedAsync(int userId, int maxRequests = 30, int windowSeconds = 60)
    {
        if (!IsAvailable) return true;
        try
        {
            var key = $"ratelimit:chat:{userId}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowStart = now - (windowSeconds * 1000L);

            var batch = GetDb().CreateBatch();
            var removeTask = batch.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
            var addTask = batch.SortedSetAddAsync(key, now.ToString(), now);
            var countTask = batch.SortedSetLengthAsync(key);
            var expireTask = batch.KeyExpireAsync(key, TimeSpan.FromSeconds(windowSeconds + 1));
            batch.Execute();

            await Task.WhenAll(removeTask, addTask, countTask, expireTask);
            return await countTask <= maxRequests;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for IsAllowedAsync user {UserId} - allowing request", userId);
            return true;
        }
    }
}
