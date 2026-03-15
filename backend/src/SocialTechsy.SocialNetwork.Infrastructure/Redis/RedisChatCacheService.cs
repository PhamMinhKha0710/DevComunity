using System.Text.Json;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;
using SocialTechsy.SocialNetwork.Infrastructure.MongoDB.Models;

namespace SocialTechsy.SocialNetwork.Infrastructure.Redis;

public class RedisChatCacheService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisChatCacheService> _logger;
    private static readonly TimeSpan ConversationTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan UserInfoTtl = TimeSpan.FromMinutes(5);

    public RedisChatCacheService(IConnectionMultiplexer redis, ILogger<RedisChatCacheService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    // ========== FAST ID GENERATION (Redis INCR ~1ms vs MongoDB counter ~5-20ms) ==========

    public async Task<long> GenerateIdAsync(string sequenceName)
    {
        return await _db.StringIncrementAsync($"chat:seq:{sequenceName}");
    }

    public async Task SyncSequenceAsync(string sequenceName, long currentMax)
    {
        var key = $"chat:seq:{sequenceName}";
        var current = (long)(await _db.StringGetAsync(key));
        if (current < currentMax)
        {
            await _db.StringSetAsync(key, currentMax);
        }
    }

    public async Task EnsureSequenceSyncedAsync(IMongoDatabase mongoDatabase)
    {
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

    // ========== UNREAD BATCH OPERATIONS ==========

    public async Task IncrementUnreadBatchAsync(IEnumerable<int> userIds, int conversationId)
    {
        var batch = _db.CreateBatch();
        var tasks = userIds.Select(uid =>
            batch.StringIncrementAsync($"chat:unread:{uid}:{conversationId}")).ToList();
        batch.Execute();
        await Task.WhenAll(tasks);
    }

    // ========== CONVERSATION CACHE ==========

    public async Task<ConversationDocument?> GetConversationAsync(int conversationId)
    {
        var json = await _db.StringGetAsync($"chat:conv:{conversationId}");
        if (!json.HasValue) return null;
        return JsonSerializer.Deserialize<ConversationDocument>(json!);
    }

    public async Task SetConversationAsync(ConversationDocument doc)
    {
        var json = JsonSerializer.Serialize(doc);
        await _db.StringSetAsync($"chat:conv:{doc.ConversationId}", json, ConversationTtl);
    }

    public async Task InvalidateConversationAsync(int conversationId)
    {
        await _db.KeyDeleteAsync($"chat:conv:{conversationId}");
    }

    // ========== USER INFO CACHE (avoids SQL Server roundtrip) ==========

    public async Task<UserInfoEmbed?> GetUserInfoAsync(int userId)
    {
        var json = await _db.StringGetAsync($"chat:user:{userId}");
        if (!json.HasValue) return null;
        return JsonSerializer.Deserialize<UserInfoEmbed>(json!);
    }

    public async Task SetUserInfoAsync(UserInfoEmbed info)
    {
        var json = JsonSerializer.Serialize(info);
        await _db.StringSetAsync($"chat:user:{info.UserId}", json, UserInfoTtl);
    }

    public async Task InvalidateUserInfoAsync(int userId)
    {
        await _db.KeyDeleteAsync($"chat:user:{userId}");
    }

    // ========== UNREAD MESSAGE COUNTS ==========

    public async Task IncrementUnreadAsync(int userId, int conversationId)
    {
        await _db.StringIncrementAsync($"chat:unread:{userId}:{conversationId}");
    }

    public async Task ResetUnreadAsync(int userId, int conversationId)
    {
        await _db.KeyDeleteAsync($"chat:unread:{userId}:{conversationId}");
    }

    public async Task<long> GetUnreadCountAsync(int userId, int conversationId)
    {
        var val = await _db.StringGetAsync($"chat:unread:{userId}:{conversationId}");
        return val.HasValue ? (long)val : 0;
    }

    // ========== TYPING INDICATOR THROTTLE + TTL ==========

    /// <summary>
    /// Returns true if the throttle key was set (i.e. event should be broadcast).
    /// Returns false if the key already exists (throttled, skip broadcast).
    /// </summary>
    public async Task<bool> SetTypingThrottleAsync(int userId, int conversationId, TimeSpan window)
    {
        var key = $"chat:typing:throttle:{userId}:{conversationId}";
        return await _db.StringSetAsync(key, "1", window, when: When.NotExists);
    }

    public async Task SetTypingAsync(int userId, int conversationId, TimeSpan ttl)
    {
        var key = $"chat:typing:active:{userId}:{conversationId}";
        await _db.StringSetAsync(key, "1", ttl);
    }

    public async Task ClearTypingAsync(int userId, int conversationId)
    {
        await _db.KeyDeleteAsync($"chat:typing:active:{userId}:{conversationId}");
        await _db.KeyDeleteAsync($"chat:typing:throttle:{userId}:{conversationId}");
    }
}
