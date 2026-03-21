using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class PostDto
{
    public int PostId { get; set; }
    public UserSummaryDto Author { get; set; } = null!;
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int LikeCount { get; set; }
    public bool UserLiked { get; set; }
    public int CommentCount { get; set; }
    public bool UserSaved { get; set; }
    public string? MediaUrls { get; set; }
    public PostVisibility Visibility { get; set; }
}
