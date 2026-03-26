using System.Text.Json;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;
using SocialTechsy.SocialNetwork.Infrastructure.MongoDB.Models;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public class RedisChatCacheService
{
    private readonly IDatabase? _db;
    private readonly ILogger<RedisChatCacheService> _logger;
    private static readonly TimeSpan ConversationTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan UserInfoTtl = TimeSpan.FromMinutes(5);

    public RedisChatCacheService(IConnectionMultiplexer redis, ILogger<RedisChatCacheService> logger)
    {
        _logger = logger;
        if (redis == null || !redis.IsConnected)
        {
            _logger.LogWarning("Redis unavailable - chat caching disabled");
            return;
        }
        try { _db = redis.GetDatabase(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Redis - chat caching disabled");
        }
    }

    private IDatabase GetDb() => _db!;
    private bool IsAvailable => _db != null;

    // ========== FAST ID GENERATION (Redis INCR ~1ms vs MongoDB counter ~5-20ms) ==========

    public async Task<long> GenerateIdAsync(string sequenceName)
    {
        if (!IsAvailable) return 0;
        return await GetDb().StringIncrementAsync($"chat:seq:{sequenceName}");
    }

    public async Task SyncSequenceAsync(string sequenceName, long currentMax)
    {
        if (!IsAvailable) return;
        var key = $"chat:seq:{sequenceName}";
        var current = (long)(await GetDb().StringGetAsync(key));
        if (current < currentMax)
        {
            await GetDb().StringSetAsync(key, currentMax);
        }
    }

    public async Task EnsureSequenceSyncedAsync(IMongoDatabase mongoDatabase)
    {
        if (!IsAvailable) return;
        try
        {
            var counters = mongoDatabase.GetCollection<CounterDocument>("counters");
            var allCounters = await counters.Find(_ => true).ToListAsync();

            foreach (var counter in allCounters)
            {
                await SyncSequenceAsync(counter.Id, counter.SequenceValue);
                _logger.LogInformation(
                    "Synced Redis sequence {Name} to {Value}", counter.Id, counter.SequenceValue);
            }

            var knownSequences = allCounters.Select(c => c.Id).ToHashSet();
            await SeedSequenceFromCollection<MessageDocument>(
                mongoDatabase, counters, knownSequences, "messages", d => d.MessageId);
            await SeedSequenceFromCollection<ConversationDocument>(
                mongoDatabase, counters, knownSequences, "conversations", d => d.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Redis sequences from MongoDB counters");
        }
    }

    private async Task SeedSequenceFromCollection<T>(
        IMongoDatabase db,
        IMongoCollection<CounterDocument> counters,
        HashSet<string> knownSequences,
        string collectionName,
        Func<T, long> idSelector)
    {
        if (knownSequences.Contains(collectionName)) return;

        var collection = db.GetCollection<T>(collectionName);
        var sort = Builders<T>.Sort.Descending("_id");
        var docs = await collection.Find(_ => true)
            .Sort(sort)
            .Limit(1)
            .ToListAsync();

        if (docs.Count == 0) return;

        var maxId = (long)idSelector(docs[0]);
        await counters.InsertOneAsync(new CounterDocument { Id = collectionName, SequenceValue = maxId });
        await SyncSequenceAsync(collectionName, maxId);
        _logger.LogInformation(
            "Seeded sequence {Name} from existing data, max _id = {Value}", collectionName, maxId);
    }

    public async Task IncrementUnreadBatchAsync(IEnumerable<int> userIds, int conversationId)
    {
        if (!IsAvailable) return;
        var batch = GetDb().CreateBatch();
        var tasks = userIds.Select(uid =>
            batch.StringIncrementAsync($"chat:unread:{uid}:{conversationId}")).ToList();
        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task<ConversationDocument?> GetConversationAsync(int conversationId)
    {
        if (!IsAvailable) return null;
        var json = await GetDb().StringGetAsync($"chat:conv:{conversationId}");
        if (!json.HasValue) return null;
        return JsonSerializer.Deserialize<ConversationDocument>(json!);
    }

    public async Task SetConversationAsync(ConversationDocument doc)
    {
        if (!IsAvailable) return;
        var json = JsonSerializer.Serialize(doc);
        await GetDb().StringSetAsync($"chat:conv:{doc.ConversationId}", json, ConversationTtl);
    }

    public async Task InvalidateConversationAsync(int conversationId)
    {
        if (!IsAvailable) return;
        await GetDb().KeyDeleteAsync($"chat:conv:{conversationId}");
    }

    public async Task<UserInfoEmbed?> GetUserInfoAsync(int userId)
    {
        if (!IsAvailable) return null;
        var json = await GetDb().StringGetAsync($"chat:user:{userId}");
        if (!json.HasValue) return null;
        return JsonSerializer.Deserialize<UserInfoEmbed>(json!);
    }

    public async Task SetUserInfoAsync(UserInfoEmbed info)
    {
        if (!IsAvailable) return;
        var json = JsonSerializer.Serialize(info);
        await GetDb().StringSetAsync($"chat:user:{info.UserId}", json, UserInfoTtl);
    }

    public async Task InvalidateUserInfoAsync(int userId)
    {
        if (!IsAvailable) return;
        await GetDb().KeyDeleteAsync($"chat:user:{userId}");
    }

    public async Task IncrementUnreadAsync(int userId, int conversationId)
    {
        if (!IsAvailable) return;
        await GetDb().StringIncrementAsync($"chat:unread:{userId}:{conversationId}");
    }

    public async Task ResetUnreadAsync(int userId, int conversationId)
    {
        if (!IsAvailable) return;
        await GetDb().KeyDeleteAsync($"chat:unread:{userId}:{conversationId}");
    }

    public async Task<long> GetUnreadCountAsync(int userId, int conversationId)
    {
        if (!IsAvailable) return 0;
        var val = await GetDb().StringGetAsync($"chat:unread:{userId}:{conversationId}");
        return val.HasValue ? (long)val : 0;
    }

    public async Task<bool> SetTypingThrottleAsync(int userId, int conversationId, TimeSpan window)
    {
        if (!IsAvailable) return false;
        var key = $"chat:typing:throttle:{userId}:{conversationId}";
        return await GetDb().StringSetAsync(key, "1", window, when: When.NotExists);
    }

    public async Task SetTypingAsync(int userId, int conversationId, TimeSpan ttl)
    {
        if (!IsAvailable) return;
        var key = $"chat:typing:active:{userId}:{conversationId}";
        await GetDb().StringSetAsync(key, "1", ttl);
    }

    public async Task ClearTypingAsync(int userId, int conversationId)
    {
        if (!IsAvailable) return;
        await GetDb().KeyDeleteAsync($"chat:typing:active:{userId}:{conversationId}");
        await GetDb().KeyDeleteAsync($"chat:typing:throttle:{userId}:{conversationId}");
    }
}
