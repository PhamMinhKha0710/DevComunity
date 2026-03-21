namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class GroupMemberDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
    public string Role { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
}
