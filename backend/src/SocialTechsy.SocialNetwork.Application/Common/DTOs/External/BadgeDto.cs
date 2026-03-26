namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.External;

/// <summary>
/// DTO for badge response
/// </summary>
public class BadgeDto
{
    public int BadgeId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string BadgeType { get; set; } = null!; // Gold, Silver, Bronze
    public int RequiredPoints { get; set; }
    public int EarnedByCount { get; set; }
}

/// <summary>
/// DTO for user badge response
/// </summary>
public class UserBadgeDto
{
    public int UserBadgeId { get; set; }
    public int BadgeId { get; set; }
    public string BadgeName { get; set; } = null!;
    public string? BadgeDescription { get; set; }
    public string? BadgeIconUrl { get; set; }
    public string BadgeType { get; set; } = null!;
    public DateTime EarnedDate { get; set; }
}
