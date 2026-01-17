namespace DevComunity.Domain.Entities;

/// <summary>
/// Notification entity - represents a user notification
/// </summary>
public class Notification
{
    public int NotificationId { get; set; }
    public string Type { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public int? FromUserId { get; set; }
    public int? QuestionId { get; set; }
    public int? AnswerId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual User? FromUser { get; set; }
}
