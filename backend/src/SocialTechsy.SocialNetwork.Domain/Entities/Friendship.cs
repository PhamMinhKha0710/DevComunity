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
    public int FriendshipId { get; private set; }
    public int RequesterId { get; private set; }
    public int AddresseeId { get; private set; }
    public FriendshipStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    // Navigation properties
    public virtual User Requester { get; set; } = null!;
    public virtual User Addressee { get; set; } = null!;

    private Friendship() { }

    public static Friendship Create(int requesterId, int addresseeId)
    {
        if (requesterId == addresseeId)
            throw new InvalidOperationException("Cannot send friend request to yourself.");

        return new Friendship
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Accept()
    {
        if (Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("Can only accept pending friend requests.");
        Status = FriendshipStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("Can only reject pending friend requests.");
        Status = FriendshipStatus.Rejected;
        RespondedAt = DateTime.UtcNow;
    }

    public void Cancel() => Reject();
}
