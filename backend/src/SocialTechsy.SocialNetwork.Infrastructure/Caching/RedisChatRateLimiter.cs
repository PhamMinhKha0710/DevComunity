using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public class RedisChatRateLimiter
{
    private readonly IDatabase _db;

    public RedisChatRateLimiter(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    /// <summary>
    /// Sliding window rate limiter using Redis sorted sets.
    /// Entries auto-expire so there's no memory leak for inactive users.
    /// Works across multiple API instances.
    /// </summary>
    public async Task<bool> IsAllowedAsync(int userId, int maxRequests = 30, int windowSeconds = 60)
    {
        var key = $"ratelimit:chat:{userId}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (windowSeconds * 1000L);

        var batch = _db.CreateBatch();
        var removeTask = batch.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
        var addTask = batch.SortedSetAddAsync(key, now.ToString(), now);
        var countTask = batch.SortedSetLengthAsync(key);
        var expireTask = batch.KeyExpireAsync(key, TimeSpan.FromSeconds(windowSeconds + 1));
        batch.Execute();

        await Task.WhenAll(removeTask, addTask, countTask, expireTask);
        return await countTask <= maxRequests;
    }
}
