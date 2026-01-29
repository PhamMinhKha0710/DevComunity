namespace DevComunity.Domain.Entities;

/// <summary>
/// Group role enum
/// </summary>
public enum GroupRole
{
    Member,
    Moderator,
    Admin
}

/// <summary>
/// Group entity - represents a community group
/// </summary>
public class Group
{
    public int GroupId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImage { get; set; }
    public int CreatorId { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
}
