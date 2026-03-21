namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class UpdateMemberRoleRequest
{
    public string Role { get; set; } = null!;  // "Admin", "Moderator", "Member"
}
