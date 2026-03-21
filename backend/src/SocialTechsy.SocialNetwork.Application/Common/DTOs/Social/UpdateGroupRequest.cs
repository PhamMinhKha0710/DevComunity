namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class UpdateGroupRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsPrivate { get; set; }
}
