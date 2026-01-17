namespace DevComunity.Domain.Entities;

/// <summary>
/// SavedItem entity - represents a saved/bookmarked item
/// </summary>
public class SavedItem
{
    public int SavedItemId { get; set; }
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
