namespace DevComunity.Domain.Entities;

/// <summary>
/// Badge entity - represents an achievement badge
/// </summary>
public class Badge
{
    public int BadgeId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string BadgeType { get; set; } = null!; // Gold, Silver, Bronze
    public int RequiredPoints { get; set; }
}

/// <summary>
/// UserBadge entity - join table for user earned badges
/// </summary>
public class UserBadge
{
    public int UserBadgeId { get; set; }
    public DateTime EarnedDate { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public int BadgeId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Badge Badge { get; set; } = null!;
}
