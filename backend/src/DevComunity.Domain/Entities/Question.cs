namespace DevComunity.Domain.Entities;

/// <summary>
/// Question entity - represents a question in the Q&A system
/// </summary>
public class Question
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public int ViewCount { get; set; }
    public int Score { get; set; }
    public string Status { get; set; } = "open";
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Foreign keys
    public int UserId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
}
