namespace DevComunity.Domain.Entities;

/// <summary>
/// Vote entity - represents a vote on a question or answer
/// </summary>
public class Vote
{
    public int VoteId { get; set; }
    public bool IsUpvote { get; set; }
    public DateTime CreatedDate { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public int? QuestionId { get; set; }
    public int? AnswerId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Question? Question { get; set; }
    public virtual Answer? Answer { get; set; }
}
