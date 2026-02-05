namespace SocialTechsy.SocialNetwork.Domain.Entities;

/// <summary>
/// Message entity - represents a chat message
/// </summary>
public class Message
{
    public int MessageId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime SentDate { get; set; }
    
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

    // Foreign keys
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    
    /// <summary>
    /// Optional: ID of the message this is replying to (for Reply/Quote feature)
    /// </summary>
    public int? ReplyToMessageId { get; set; }

    // Navigation properties
    public virtual Conversation Conversation { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
    
    /// <summary>
    /// The message this is a reply to - like Instagram/Facebook reply feature
    /// </summary>
    public virtual Message? ReplyToMessage { get; set; }
    
    /// <summary>
    /// Reactions (emoji) on this message - like Facebook/Instagram
    /// </summary>
    public virtual ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
}
