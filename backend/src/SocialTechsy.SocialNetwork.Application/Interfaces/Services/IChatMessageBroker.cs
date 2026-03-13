namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface IChatMessageBroker
{
    Task PublishAsync(ChatEvent chatEvent);
}

public class ChatEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public static class ChatEventTypes
{
    public const string NewMessage = "message.new";
    public const string MessagesRead = "message.read";
    public const string ReactionAdded = "reaction.add";
    public const string ReactionRemoved = "reaction.remove";
}

public class NewMessagePayload
{
    public int ConversationId { get; set; }
    public long MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = null!;
    public List<int> ParticipantUserIds { get; set; } = new();
}

public class MessagesReadPayload
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
}
