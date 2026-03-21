namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class FriendshipStatusDto
{
    public bool AreFriends { get; set; }
    public bool RequestPending { get; set; }
    public int? FriendshipId { get; set; }
    public bool IsSentByMe { get; set; }
}
