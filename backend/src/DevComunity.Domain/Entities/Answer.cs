namespace DevComunity.Domain.Entities;

/// <summary>
/// Answer entity - represents an answer to a question
/// </summary>
public class Answer
{
    public int AnswerId { get; set; }
    public string Body { get; set; } = null!;
    public int Score { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Foreign keys
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public int? ParentAnswerId { get; set; }

    // Navigation properties
    public virtual Question Question { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Answer? ParentAnswer { get; set; }
    public virtual ICollection<Answer> ChildAnswers { get; set; } = new List<Answer>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
