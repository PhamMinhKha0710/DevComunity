namespace DevComunity.Domain.Entities;

/// <summary>
/// ConversationParticipant entity - join table for conversation members
/// </summary>
public class ConversationParticipant
{
    public int ConversationParticipantId { get; set; }
    public DateTime JoinedDate { get; set; }
    public DateTime? LastReadDate { get; set; }

    // Foreign keys
    public int ConversationId { get; set; }
    public int UserId { get; set; }

    // Navigation properties
    public virtual Conversation Conversation { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
