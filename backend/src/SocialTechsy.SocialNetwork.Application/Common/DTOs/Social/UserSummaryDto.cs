namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class UserSummaryDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
}
