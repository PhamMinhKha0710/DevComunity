namespace SocialTechsy.SocialNetwork.Application.Common.DTOs;

/// <summary>
/// DTO for conversation list response
/// </summary>
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

/// <summary>
/// DTO for conversation participant
/// </summary>
public class ConversationParticipantDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
}

/// <summary>
/// DTO for message response
/// </summary>
public class MessageDto
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = null!;
    public string? SenderProfilePicture { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentDate { get; set; }
    public bool IsRead { get; set; }
    
    /// <summary>
    /// Message type: text, image, video, audio, file
    /// </summary>
    public string MessageType { get; set; } = "text";
    
    /// <summary>
    /// URL to the attachment (for media messages)
    /// </summary>
    public string? AttachmentUrl { get; set; }
    
    /// <summary>
    /// Original filename of the attachment
    /// </summary>
    public string? AttachmentFileName { get; set; }
    
    /// <summary>
    /// Size of the attachment in bytes
    /// </summary>
    public long? AttachmentSize { get; set; }
    
    /// <summary>
    /// Reply/Quote - ID of message being replied to (null if not a reply)
    /// </summary>
    public int? ReplyToMessageId { get; set; }
    
    /// <summary>
    /// Reply/Quote - Info about the message being replied to
    /// </summary>
    public ReplyToMessageDto? ReplyToMessage { get; set; }
    
    /// <summary>
    /// Reactions on this message - Instagram/Facebook style
    /// </summary>
    public List<MessageReactionDto> Reactions { get; set; } = new();
}

/// <summary>
/// DTO for the message being replied to (simplified)
/// </summary>
public class ReplyToMessageDto
{
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = null!;
    public string Content { get; set; } = null!;
}

/// <summary>
/// DTO for message reaction
/// </summary>
public class MessageReactionDto
{
    public int MessageReactionId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? ProfilePicture { get; set; }
    public string ReactionType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for adding a reaction to a message
/// </summary>
public class AddReactionDto
{
    /// <summary>
    /// Reaction type: like, love, haha, wow, sad, angry
    /// </summary>
    public string ReactionType { get; set; } = null!;
}

/// <summary>
/// DTO for reaction summary (grouped by type)
/// </summary>
public class ReactionSummaryDto
{
    public string ReactionType { get; set; } = null!;
    public int Count { get; set; }
    public List<string> RecentUsers { get; set; } = new();
}
