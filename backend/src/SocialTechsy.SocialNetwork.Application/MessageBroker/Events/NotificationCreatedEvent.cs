namespace SocialTechsy.SocialNetwork.Application.MessageBroker.Events;

public record NotificationCreatedEvent
{
    public int ActorId { get; init; }
    public int ReceiverId { get; init; }
    public string Type { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string? Link { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
