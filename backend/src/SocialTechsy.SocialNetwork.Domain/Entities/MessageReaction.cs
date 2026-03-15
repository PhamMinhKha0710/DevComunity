namespace SocialTechsy.SocialNetwork.Domain.Entities;

public class MessageReaction
{
    public static readonly string[] ValidReactionTypes = { "like", "love", "haha", "wow", "sad", "angry" };

    public int MessageReactionId { get; set; }
    public string ReactionType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public long MessageId { get; set; }
    public int UserId { get; set; }

    public virtual Message Message { get; set; } = null!;
    public virtual User User { get; set; } = null!;

    public static MessageReaction Create(long messageId, int userId, string reactionType)
    {
        var normalized = reactionType.ToLower();
        if (!ValidReactionTypes.Contains(normalized))
            throw new ArgumentException($"Invalid reaction type: {reactionType}. Valid types: {string.Join(", ", ValidReactionTypes)}.");

        return new MessageReaction
        {
            MessageId = messageId,
            UserId = userId,
            ReactionType = normalized,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateType(string reactionType)
    {
        var normalized = reactionType.ToLower();
        if (!ValidReactionTypes.Contains(normalized))
            throw new ArgumentException($"Invalid reaction type: {reactionType}.");

        ReactionType = normalized;
        CreatedAt = DateTime.UtcNow;
    }
}
