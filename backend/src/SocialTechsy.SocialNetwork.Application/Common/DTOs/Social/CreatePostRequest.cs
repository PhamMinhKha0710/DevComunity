using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class CreatePostRequest
{
    public string Content { get; set; } = null!;
    public int? GroupId { get; set; }
    public string? MediaUrls { get; set; }
    public PostVisibility Visibility { get; set; }
}
