namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class UpdatePostRequest
{
    public string Content { get; set; } = null!;
    public string? MediaUrls { get; set; }
}
