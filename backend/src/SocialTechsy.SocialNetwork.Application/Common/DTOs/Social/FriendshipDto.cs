using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class FriendshipDto
{
    public int FriendshipId { get; set; }
    public UserSummaryDto Requester { get; set; } = null!;
    public UserSummaryDto Addressee { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
