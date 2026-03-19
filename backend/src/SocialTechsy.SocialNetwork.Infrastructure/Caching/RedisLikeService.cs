using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public class RedisLikeService : ILikeService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisLikeService> _logger;
    private static readonly TimeSpan CounterTtl = TimeSpan.FromDays(1);

    public RedisLikeService(IConnectionMultiplexer redis, ILogger<RedisLikeService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    private static string CountKey(string targetType, int targetId) =>
        $"like:{targetType}:{targetId}:count";

    private static string UsersKey(string targetType, int targetId) =>
        $"like:{targetType}:{targetId}:users";

    public async Task<long> LikeAsync(string targetType, int targetId, int userId)
    {
        var added = await _db.SetAddAsync(UsersKey(targetType, targetId), userId);
        if (added)
        {
            var count = await _db.StringIncrementAsync(CountKey(targetType, targetId));
            await _db.KeyExpireAsync(CountKey(targetType, targetId), CounterTtl);
            await _db.KeyExpireAsync(UsersKey(targetType, targetId), CounterTtl);
            _logger.LogDebug("Like added: {TargetType}:{TargetId} by user {UserId}, count={Count}", targetType, targetId, userId, count);
            return count;
        }
        return await GetLikeCountAsync(targetType, targetId);
    }

    public async Task<long> UnlikeAsync(string targetType, int targetId, int userId)
    {
        var removed = await _db.SetRemoveAsync(UsersKey(targetType, targetId), userId);
        if (removed)
        {
            var count = await _db.StringDecrementAsync(CountKey(targetType, targetId));
            if (count < 0)
            {
                await _db.StringSetAsync(CountKey(targetType, targetId), 0);
                count = 0;
            }
            _logger.LogDebug("Like removed: {TargetType}:{TargetId} by user {UserId}, count={Count}", targetType, targetId, userId, count);
            return count;
        }
        return await GetLikeCountAsync(targetType, targetId);
    }

    public async Task<bool> IsLikedAsync(string targetType, int targetId, int userId)
    {
        return await _db.SetContainsAsync(UsersKey(targetType, targetId), userId);
    }

    public async Task<long> GetLikeCountAsync(string targetType, int targetId)
    {
        var val = await _db.StringGetAsync(CountKey(targetType, targetId));
        return val.HasValue ? (long)val : 0;
    }

    public async Task<Dictionary<int, long>> GetLikeCountsBatchAsync(string targetType, int[] targetIds)
    {
        var result = new Dictionary<int, long>(targetIds.Length);
        if (targetIds.Length == 0) return result;

        var keys = targetIds.Select(id => (RedisKey)CountKey(targetType, id)).ToArray();
        var values = await _db.StringGetAsync(keys);

        for (var i = 0; i < targetIds.Length; i++)
        {
            result[targetIds[i]] = values[i].HasValue ? (long)values[i] : 0;
        }
        return result;
    }

    public async Task SyncFromSqlAsync(string targetType, int targetId, int sqlScore)
    {
        var redisCount = await GetLikeCountAsync(targetType, targetId);
        if (redisCount == 0 && sqlScore > 0)
        {
            await _db.StringSetAsync(CountKey(targetType, targetId), sqlScore);
        }
    }
}
