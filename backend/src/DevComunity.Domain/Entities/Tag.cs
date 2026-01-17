namespace DevComunity.Domain.Entities;

/// <summary>
/// Tag entity - represents a tag for categorizing questions
/// </summary>
public class Tag
{
    public int TagId { get; set; }
    public string TagName { get; set; } = null!;
    public string? Description { get; set; }
    public int UsageCount { get; set; }

    // Navigation properties
    public virtual ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
}
