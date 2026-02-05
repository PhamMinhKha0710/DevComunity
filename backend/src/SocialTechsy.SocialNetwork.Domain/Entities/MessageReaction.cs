namespace SocialTechsy.SocialNetwork.Domain.Entities;

/// <summary>
/// MessageReaction entity - represents a reaction (emoji) on a chat message
/// Similar to Facebook/Instagram message reactions
/// </summary>
public class MessageReaction
{
    public int MessageReactionId { get; set; }
    
    /// <summary>
    /// The type of reaction: like, love, haha, wow, sad, angry
    /// </summary>
    public string ReactionType { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }

    // Foreign keys
    public int MessageId { get; set; }
    public int UserId { get; set; }

    // Navigation properties
    public virtual Message Message { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
