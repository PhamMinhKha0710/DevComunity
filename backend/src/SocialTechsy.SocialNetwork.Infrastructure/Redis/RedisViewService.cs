using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Redis;

public class RedisViewService : IViewService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisViewService> _logger;

    public RedisViewService(IConnectionMultiplexer redis, ILogger<RedisViewService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    private static string CountKey(int questionId) => $"view:question:{questionId}";
    private static string VisitorsKey(int questionId) => $"view:question:{questionId}:visitors";
    private static string DeltaKey(int questionId) => $"view:question:{questionId}:delta";

    public async Task<long> IncrementViewAsync(int questionId, string? userId, string? ip)
    {
        var identifier = userId ?? ip ?? "anonymous";
        var added = await _db.HyperLogLogAddAsync(VisitorsKey(questionId), identifier);

        await _db.StringIncrementAsync(DeltaKey(questionId));
        var total = await _db.StringIncrementAsync(CountKey(questionId));

        _logger.LogDebug("View recorded: question {QuestionId}, unique={IsUnique}, total={Total}", questionId, added, total);
        return total;
    }

    public async Task<long> GetViewCountAsync(int questionId)
    {
        var val = await _db.StringGetAsync(CountKey(questionId));
        return val.HasValue ? (long)val : 0;
    }

    public async Task<Dictionary<int, long>> GetViewCountsBatchAsync(int[] questionIds)
    {
        var result = new Dictionary<int, long>(questionIds.Length);
        if (questionIds.Length == 0) return result;

        var keys = questionIds.Select(id => (RedisKey)CountKey(id)).ToArray();
        var values = await _db.StringGetAsync(keys);

        for (var i = 0; i < questionIds.Length; i++)
        {
            result[questionIds[i]] = values[i].HasValue ? (long)values[i] : 0;
        }
        return result;
    }

    public async Task<long> GetAndResetDeltaAsync(int questionId)
    {
        var delta = await _db.StringGetSetAsync(DeltaKey(questionId), 0);
        return delta.HasValue ? (long)delta : 0;
    }

    public async Task<RedisKey[]> GetAllDeltaKeysAsync()
    {
        var server = _db.Multiplexer.GetServers().First();
        var keys = new List<RedisKey>();
        await foreach (var key in server.KeysAsync(pattern: "view:question:*:delta"))
        {
            keys.Add(key);
        }
        return keys.ToArray();
    }

    public async Task InitializeFromSqlAsync(int questionId, int sqlViewCount)
    {
        var exists = await _db.KeyExistsAsync(CountKey(questionId));
        if (!exists && sqlViewCount > 0)
        {
            await _db.StringSetAsync(CountKey(questionId), sqlViewCount);
        }
    }
}
