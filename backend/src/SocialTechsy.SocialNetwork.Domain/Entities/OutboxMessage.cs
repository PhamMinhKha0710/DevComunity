namespace SocialTechsy.SocialNetwork.Domain.Entities;

public class OutboxMessage
{
    public long Id { get; set; }
    public string EventType { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
}
