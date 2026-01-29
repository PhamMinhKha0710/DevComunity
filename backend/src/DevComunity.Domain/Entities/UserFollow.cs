namespace DevComunity.Domain.Entities;

/// <summary>
/// UserFollow entity - represents a follow relationship between users
/// </summary>
public class UserFollow
{
    public int FollowerId { get; set; }   // Người theo dõi
    public int FollowingId { get; set; }  // Người được theo dõi
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual User Follower { get; set; } = null!;
    public virtual User Following { get; set; } = null!;
}
