using MongoDB.Bson.Serialization.Attributes;

namespace SocialTechsy.SocialNetwork.Infrastructure.MongoDB.Models;

public class CounterDocument
{
    [BsonId]
    public string Id { get; set; } = null!;
    public long SequenceValue { get; set; }
}

public class UserInfoEmbed
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
}

public class ConversationDocument
{
    [BsonId]
    public int ConversationId { get; set; }
    public string? Title { get; set; }
    public bool IsGroupChat { get; set; }
    public string GroupTier { get; set; } = "small";
    public int ParticipantCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastMessageDate { get; set; }
    public List<ParticipantEmbed> Participants { get; set; } = new();

    public static string DetermineGroupTier(int participantCount) => participantCount switch
    {
        <= 50 => "small",
        <= 500 => "medium",
        _ => "large"
    };
}

public class ParticipantEmbed
{
    public int ConversationParticipantId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedDate { get; set; }
    public DateTime? LastReadDate { get; set; }
    public int LastReadMessageId { get; set; }
    public UserInfoEmbed User { get; set; } = null!;
}

public class MessageDocument
{
    [BsonId]
    public long MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public UserInfoEmbed Sender { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime SentDate { get; set; }
    public string MessageType { get; set; } = "text";
    public int DeliveryStatus { get; set; } = 0;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public long? AttachmentSize { get; set; }
    public long? ReplyToMessageId { get; set; }
    public List<ReactionEmbed> Reactions { get; set; } = new();
}

public class ReactionEmbed
{
    public int MessageReactionId { get; set; }
    public int UserId { get; set; }
    public UserInfoEmbed User { get; set; } = null!;
    public string ReactionType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
