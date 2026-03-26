using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public class RedisLikeService : ILikeService
{
    private readonly IDatabase? _db;
    private readonly ILogger<RedisLikeService> _logger;
    private static readonly TimeSpan CounterTtl = TimeSpan.FromDays(1);

    public RedisLikeService(IConnectionMultiplexer redis, ILogger<RedisLikeService> logger)
    {
        _logger = logger;
        if (redis == null || !redis.IsConnected)
        {
            _logger.LogWarning("Redis unavailable - like counts will return 0");
            return;
        }
        try { _db = redis.GetDatabase(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Redis database - like counts will return 0");
        }
    }

    private IDatabase GetDb() => _db!;

    private bool IsAvailable => _db != null;

    private static string CountKey(string targetType, int targetId) =>
        $"like:{targetType}:{targetId}:count";

    private static string UsersKey(string targetType, int targetId) =>
        $"like:{targetType}:{targetId}:users";

    public async Task<long> LikeAsync(string targetType, int targetId, int userId)
    {
        if (!IsAvailable) return 0;
        var added = await GetDb().SetAddAsync(UsersKey(targetType, targetId), userId);
        if (added)
        {
            var count = await GetDb().StringIncrementAsync(CountKey(targetType, targetId));
            await GetDb().KeyExpireAsync(CountKey(targetType, targetId), CounterTtl);
            await GetDb().KeyExpireAsync(UsersKey(targetType, targetId), CounterTtl);
            _logger.LogDebug("Like added: {TargetType}:{TargetId} by user {UserId}, count={Count}", targetType, targetId, userId, count);
            return count;
        }
        return await GetLikeCountAsync(targetType, targetId);
    }

    public async Task<long> UnlikeAsync(string targetType, int targetId, int userId)
    {
        if (!IsAvailable) return 0;
        var removed = await GetDb().SetRemoveAsync(UsersKey(targetType, targetId), userId);
        if (removed)
        {
            var count = await GetDb().StringDecrementAsync(CountKey(targetType, targetId));
            if (count < 0)
            {
                await GetDb().StringSetAsync(CountKey(targetType, targetId), 0);
                count = 0;
            }
            _logger.LogDebug("Like removed: {TargetType}:{TargetId} by user {UserId}, count={Count}", targetType, targetId, userId, count);
            return count;
        }
        return await GetLikeCountAsync(targetType, targetId);
    }

    public async Task<bool> IsLikedAsync(string targetType, int targetId, int userId)
    {
        if (!IsAvailable) return false;
        try
        {
            return await GetDb().SetContainsAsync(UsersKey(targetType, targetId), userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for IsLikedAsync {TargetType}:{TargetId}", targetType, targetId);
            return false;
        }
    }

    public async Task<long> GetLikeCountAsync(string targetType, int targetId)
    {
        if (!IsAvailable) return 0;
        try
        {
            var val = await GetDb().StringGetAsync(CountKey(targetType, targetId));
            return val.HasValue ? (long)val : 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetLikeCountAsync {TargetType}:{TargetId}", targetType, targetId);
            return 0;
        }
    }

    public async Task<Dictionary<int, long>> GetLikeCountsBatchAsync(string targetType, int[] targetIds)
    {
        var result = new Dictionary<int, long>(targetIds.Length);
        if (targetIds.Length == 0) return result;
        if (!IsAvailable)
        {
            foreach (var id in targetIds)
                result.TryAdd(id, 0);
            return result;
        }

        try
        {
            var keys = targetIds.Select(id => (RedisKey)CountKey(targetType, id)).ToArray();
            var values = await GetDb().StringGetAsync(keys);

            for (var i = 0; i < targetIds.Length; i++)
            {
                result[targetIds[i]] = values[i].HasValue ? (long)values[i] : 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetLikeCountsBatchAsync, returning 0 counts");
            foreach (var id in targetIds)
                result.TryAdd(id, 0);
        }
        return result;
    }

    public async Task SyncFromSqlAsync(string targetType, int targetId, int sqlScore)
    {
        if (!IsAvailable) return;
        try
        {
            var redisCount = await GetLikeCountAsync(targetType, targetId);
            if (redisCount == 0 && sqlScore > 0)
            {
                await GetDb().StringSetAsync(CountKey(targetType, targetId), sqlScore);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for SyncFromSqlAsync {TargetType}:{TargetId}", targetType, targetId);
        }
    }
}
