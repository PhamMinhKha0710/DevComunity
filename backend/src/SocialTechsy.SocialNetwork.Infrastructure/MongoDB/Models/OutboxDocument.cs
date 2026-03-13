using MongoDB.Bson.Serialization.Attributes;

namespace SocialTechsy.SocialNetwork.Infrastructure.MongoDB.Models;

public class OutboxDocument
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Processed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
}
