namespace SocialTechsy.SocialNetwork.Domain.Entities;

/// <summary>
/// Vote entity - represents a vote on a question or answer
/// </summary>
public class Vote
{
    public int VoteId { get; private set; }
    public bool IsUpvote { get; private set; }
    public DateTime CreatedDate { get; private set; }

    // Foreign keys
    public int UserId { get; private set; }
    public int? QuestionId { get; private set; }
    public int? AnswerId { get; private set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Question? Question { get; set; }
    public virtual Answer? Answer { get; set; }

    private Vote() { }

    public static Vote Create(int userId, bool isUpvote, int? questionId = null, int? answerId = null)
    {
        if (questionId == null && answerId == null)
            throw new ArgumentException("Vote must target either a question or an answer.");

        return new Vote
        {
            UserId = userId,
            IsUpvote = isUpvote,
            QuestionId = questionId,
            AnswerId = answerId,
            CreatedDate = DateTime.UtcNow
        };
    }

    public void ChangeDirection(bool isUpvote) => IsUpvote = isUpvote;
}
