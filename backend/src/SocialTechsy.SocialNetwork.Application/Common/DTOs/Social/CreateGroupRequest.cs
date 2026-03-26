namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class CreateGroupRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
}
