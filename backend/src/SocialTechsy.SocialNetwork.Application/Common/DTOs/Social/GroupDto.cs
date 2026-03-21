namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class GroupDto
{
    public int GroupId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImage { get; set; }
    public UserSummaryDto Creator { get; set; } = null!;
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public bool IsMember { get; set; }
    public string? CurrentUserRole { get; set; }
}
