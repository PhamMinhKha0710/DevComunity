namespace DevComunity.Domain.Entities;

/// <summary>
/// Message entity - represents a chat message
/// </summary>
public class Message
{
    public int MessageId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime SentDate { get; set; }

    // Foreign keys
    public int ConversationId { get; set; }
    public int SenderId { get; set; }

    // Navigation properties
    public virtual Conversation Conversation { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
}
