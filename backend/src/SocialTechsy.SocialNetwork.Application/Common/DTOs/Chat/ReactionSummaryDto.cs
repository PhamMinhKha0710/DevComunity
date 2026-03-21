namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Chat;

public class ReactionSummaryDto
{
    public string ReactionType { get; set; } = null!;
    public int Count { get; set; }
    public List<string> RecentUsers { get; set; } = new();
}
