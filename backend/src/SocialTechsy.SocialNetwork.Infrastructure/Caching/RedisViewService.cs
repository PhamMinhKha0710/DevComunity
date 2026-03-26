using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public class RedisViewService : IViewService
{
    private readonly IDatabase? _db;
    private readonly ILogger<RedisViewService> _logger;
    private static readonly TimeSpan CounterTtl = TimeSpan.FromDays(7);

    public RedisViewService(IConnectionMultiplexer redis, ILogger<RedisViewService> logger)
    {
        _logger = logger;
        if (redis == null || !redis.IsConnected)
        {
            _logger.LogWarning("Redis unavailable - view counts disabled");
            return;
        }
        try { _db = redis.GetDatabase(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Redis - view counts disabled");
        }
    }

    private IDatabase GetDb() => _db!;
    private bool IsAvailable => _db != null;

    private const string DeltaTrackingSet = "view:delta:tracking";

    private static string CountKey(int questionId) => $"view:question:{questionId}";
    private static string VisitorsKey(int questionId) => $"view:question:{questionId}:visitors";
    private static string DeltaKey(int questionId) => $"view:question:{questionId}:delta";

    public async Task<long> IncrementViewAsync(int questionId, string? userId, string? ip)
    {
        if (!IsAvailable) return 0;
        var identifier = userId ?? ip ?? "anonymous";
        var added = await GetDb().HyperLogLogAddAsync(VisitorsKey(questionId), identifier);

        await GetDb().StringIncrementAsync(DeltaKey(questionId));
        await GetDb().SetAddAsync(DeltaTrackingSet, questionId);
        var total = await GetDb().StringIncrementAsync(CountKey(questionId));

        _logger.LogDebug("View recorded: question {QuestionId}, unique={IsUnique}, total={Total}", questionId, added, total);
        return total;
    }

    public async Task<long> GetViewCountAsync(int questionId)
    {
        if (!IsAvailable) return 0;
        var val = await GetDb().StringGetAsync(CountKey(questionId));
        return val.HasValue ? (long)val : 0;
    }

    public async Task<Dictionary<int, long>> GetViewCountsBatchAsync(int[] questionIds)
    {
        var result = new Dictionary<int, long>(questionIds.Length);
        if (questionIds.Length == 0) return result;
        if (!IsAvailable)
        {
            foreach (var id in questionIds) result.TryAdd(id, 0);
            return result;
        }

        var keys = questionIds.Select(id => (RedisKey)CountKey(id)).ToArray();
        var values = await GetDb().StringGetAsync(keys);

        for (var i = 0; i < questionIds.Length; i++)
        {
            result[questionIds[i]] = values[i].HasValue ? (long)values[i] : 0;
        }
        return result;
    }

    public async Task<long> GetAndResetDeltaAsync(int questionId)
    {
        if (!IsAvailable) return 0;
        var delta = await GetDb().StringGetSetAsync(DeltaKey(questionId), 0);
        return delta.HasValue ? (long)delta : 0;
    }

    public async Task<int[]> GetDirtyQuestionIdsAsync()
    {
        if (!IsAvailable) return Array.Empty<int>();
        var members = await GetDb().SetMembersAsync(DeltaTrackingSet);
        return members
            .Where(m => m.HasValue)
            .Select(m => (int)m)
            .ToArray();
    }

    public async Task RemoveFromTrackingAsync(int questionId)
    {
        if (!IsAvailable) return;
        await GetDb().SetRemoveAsync(DeltaTrackingSet, questionId);
    }

    public async Task InitializeFromSqlAsync(int questionId, int sqlViewCount)
    {
        if (!IsAvailable) return;
        try
        {
            var exists = await GetDb().KeyExistsAsync(CountKey(questionId));
            if (!exists && sqlViewCount > 0)
            {
                await GetDb().StringSetAsync(CountKey(questionId), sqlViewCount, CounterTtl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for InitializeFromSqlAsync question {QuestionId}", questionId);
        }
    }
}
