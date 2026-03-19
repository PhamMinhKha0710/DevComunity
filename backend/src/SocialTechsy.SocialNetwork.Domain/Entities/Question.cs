namespace SocialTechsy.SocialNetwork.Domain.Entities;

/// <summary>
/// Question entity - represents a question in the Q&A system
/// </summary>
public class Question
{
    public int QuestionId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public int ViewCount { get; private set; }
    public int Score { get; private set; }
    public string Status { get; private set; } = "open";
    public DateTime CreatedDate { get; private set; }
    public DateTime? UpdatedDate { get; private set; }

    // Foreign keys
    public int UserId { get; private set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();

    private Question() { }

    public static Question Create(int userId, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 10)
            throw new ArgumentException("Title must be at least 10 characters.", nameof(title));
        if (string.IsNullOrWhiteSpace(body) || body.Length < 30)
            throw new ArgumentException("Body must be at least 30 characters.", nameof(body));

        return new Question
        {
            Title = title,
            Body = body,
            UserId = userId,
            CreatedDate = DateTime.UtcNow,
            Status = "open",
            ViewCount = 0,
            Score = 0
        };
    }

    public void Update(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 10)
            throw new ArgumentException("Title must be at least 10 characters.", nameof(title));
        if (string.IsNullOrWhiteSpace(body) || body.Length < 30)
            throw new ArgumentException("Body must be at least 30 characters.", nameof(body));

        Title = title;
        Body = body;
        UpdatedDate = DateTime.UtcNow;
    }

    public void IncrementViewCount() => ViewCount++;

    public void AddScore(int delta) => Score += delta;

    public void Close()
    {
        Status = "closed";
        UpdatedDate = DateTime.UtcNow;
    }

    public void Reopen()
    {
        Status = "open";
        UpdatedDate = DateTime.UtcNow;
    }
}
