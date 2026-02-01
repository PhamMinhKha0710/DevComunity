namespace DevComunity.Domain.Entities;

/// <summary>
/// GroupMember entity - represents a user's membership in a group
/// </summary>
public class GroupMember
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public GroupRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    // Navigation properties
    public virtual Group Group { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
