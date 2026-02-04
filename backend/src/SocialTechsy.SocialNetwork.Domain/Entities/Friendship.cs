namespace SocialTechsy.SocialNetwork.Domain.Entities;

/// <summary>
/// Friendship status enum
/// </summary>
public enum FriendshipStatus
{
    Pending,
    Accepted,
    Rejected
}

/// <summary>
/// Friendship entity - represents a friend request/relationship between two users
/// </summary>
public class Friendship
{
    public int FriendshipId { get; set; }
    public int RequesterId { get; set; }  // Người gửi yêu cầu
    public int AddresseeId { get; set; }  // Người nhận yêu cầu
    public FriendshipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    // Navigation properties
    public virtual User Requester { get; set; } = null!;
    public virtual User Addressee { get; set; } = null!;
}
