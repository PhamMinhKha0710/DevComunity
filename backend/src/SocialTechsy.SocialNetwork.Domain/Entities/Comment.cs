namespace SocialTechsy.SocialNetwork.Domain.Entities;

/// <summary>
/// Comment entity - represents a comment on a question, answer, or post
/// </summary>
public class Comment
{
    public int CommentId { get; set; }
    public string Body { get; set; } = null!;
    public DateTime CreatedDate { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public int? QuestionId { get; set; }
    public int? AnswerId { get; set; }
    public int? PostId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Question? Question { get; set; }
    public virtual Answer? Answer { get; set; }
    public virtual Post? Post { get; set; }
}
