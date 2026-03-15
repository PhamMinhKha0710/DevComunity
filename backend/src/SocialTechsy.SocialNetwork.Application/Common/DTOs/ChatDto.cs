using System.Text.Json.Serialization;

namespace SocialTechsy.SocialNetwork.Application.Common.DTOs;

public class ConversationDto
{
    public int ConversationId { get; set; }
    public string? Title { get; set; }
    public bool IsGroupChat { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastMessageDate { get; set; }
    public string? LastMessagePreview { get; set; }
    public int UnreadCount { get; set; }
    public List<ConversationParticipantDto> Participants { get; set; } = new();
}

public class ConversationParticipantDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
}

public class MessageDto
{
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public long MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = null!;
    public string? SenderProfilePicture { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentDate { get; set; }
    public bool IsRead { get; set; }
    public string MessageType { get; set; } = "text";
    public string DeliveryStatus { get; set; } = "sent";
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public long? AttachmentSize { get; set; }

    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public long? ReplyToMessageId { get; set; }
    public ReplyToMessageDto? ReplyToMessage { get; set; }
    public List<MessageReactionDto> Reactions { get; set; } = new();
}

public class ReplyToMessageDto
{
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public long MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = null!;
    public string Content { get; set; } = null!;
}

public class MessageReactionDto
{
    public int MessageReactionId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? ProfilePicture { get; set; }
    public string ReactionType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class AddReactionDto
{
    public string ReactionType { get; set; } = null!;
}

public class ReactionSummaryDto
{
    public string ReactionType { get; set; } = null!;
    public int Count { get; set; }
    public List<string> RecentUsers { get; set; } = new();
}
