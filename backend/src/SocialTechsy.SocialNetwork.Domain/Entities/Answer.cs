namespace SocialTechsy.SocialNetwork.Domain.Entities;

/// <summary>
/// Answer entity - represents an answer to a question
/// </summary>
public class Answer
{
    public int AnswerId { get; private set; }
    public string Body { get; private set; } = null!;
    public int Score { get; private set; }
    public bool IsAccepted { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? UpdatedDate { get; private set; }

    // Foreign keys
    public int QuestionId { get; private set; }
    public int UserId { get; private set; }
    public int? ParentAnswerId { get; private set; }

    // Navigation properties
    public virtual Question Question { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Answer? ParentAnswer { get; set; }
    public virtual ICollection<Answer> ChildAnswers { get; set; } = new List<Answer>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    private Answer() { }

    public static Answer Create(int questionId, int userId, string body, int? parentAnswerId = null)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length < 30)
            throw new ArgumentException("Body must be at least 30 characters.", nameof(body));

        return new Answer
        {
            Body = body,
            QuestionId = questionId,
            UserId = userId,
            ParentAnswerId = parentAnswerId,
            CreatedDate = DateTime.UtcNow,
            Score = 0,
            IsAccepted = false
        };
    }

    public void Update(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length < 30)
            throw new ArgumentException("Body must be at least 30 characters.", nameof(body));

        Body = body;
        UpdatedDate = DateTime.UtcNow;
    }

    public void Accept()
    {
        if (IsAccepted)
            throw new InvalidOperationException("Answer is already accepted.");
        IsAccepted = true;
    }

    public void Unaccept() => IsAccepted = false;

    public void AddScore(int delta) => Score += delta;
}
