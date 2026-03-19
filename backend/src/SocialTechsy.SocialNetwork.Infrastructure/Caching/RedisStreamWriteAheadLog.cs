using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

/// <summary>
/// Redis Streams-based write-ahead log for chat messages.
/// Messages are written to a Redis Stream before MongoDB persistence,
/// enabling sub-5ms delivery with async MongoDB writes.
/// On server crash, uncommitted entries are replayed from the stream.
/// </summary>
public class RedisStreamWriteAheadLog
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisStreamWriteAheadLog> _logger;
    private static readonly TimeSpan StreamRetention = TimeSpan.FromHours(1);

    public RedisStreamWriteAheadLog(IConnectionMultiplexer redis, ILogger<RedisStreamWriteAheadLog> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<string> AppendAsync(int conversationId, WalEntry entry)
    {
        var key = $"chat:wal:{conversationId}";
        var payload = JsonSerializer.Serialize(entry);
        var id = await _db.StreamAddAsync(key, new[]
        {
            new NameValueEntry("messageId", entry.MessageId.ToString()),
            new NameValueEntry("senderId", entry.SenderId.ToString()),
            new NameValueEntry("payload", payload)
        });
        await _db.KeyExpireAsync(key, StreamRetention);
        return id!;
    }

    public async Task AcknowledgeAsync(int conversationId, string streamEntryId)
    {
        try
        {
            var key = $"chat:wal:{conversationId}";
            await _db.StreamDeleteAsync(key, new[] { (RedisValue)streamEntryId });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete WAL entry {EntryId} for conversation {ConversationId}",
                streamEntryId, conversationId);
        }
    }

    public async Task<List<WalEntry>> GetUncommittedAsync(int conversationId, int maxCount = 100)
    {
        var key = $"chat:wal:{conversationId}";
        var entries = await _db.StreamReadAsync(key, "0-0", maxCount);
        if (entries == null || entries.Length == 0)
            return new List<WalEntry>();

        var result = new List<WalEntry>();
        foreach (var entry in entries)
        {
            var payload = entry.Values.FirstOrDefault(v => v.Name == "payload").Value;
            if (payload.HasValue)
            {
                var walEntry = JsonSerializer.Deserialize<WalEntry>(payload!);
                if (walEntry != null)
                {
                    walEntry.StreamEntryId = entry.Id!;
                    result.Add(walEntry);
                }
            }
        }
        return result;
    }

    public async Task<long> GetPendingCountAsync(int conversationId)
    {
        var key = $"chat:wal:{conversationId}";
        return await _db.StreamLengthAsync(key);
    }
}

public class WalEntry
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = null!;
    public string MessageType { get; set; } = "text";
    public DateTime SentDate { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public long? AttachmentSize { get; set; }
    public int? ReplyToMessageId { get; set; }
    public string? StreamEntryId { get; set; }
}
