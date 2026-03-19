namespace SocialTechsy.SocialNetwork.Domain.Entities;

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
    public int GroupId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? CoverImage { get; private set; }
    public int CreatorId { get; private set; }
    public bool IsPrivate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    private Group() { }

    public static Group Create(int creatorId, string name, string? description, bool isPrivate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name cannot be empty.", nameof(name));

        return new Group
        {
            CreatorId = creatorId,
            Name = name,
            Description = description,
            IsPrivate = isPrivate,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateInfo(string? name, string? description, bool? isPrivate)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name;
        }
        Description = description;
        if (isPrivate.HasValue)
            IsPrivate = isPrivate.Value;
    }

    public void UpdatePrivacy(bool isPrivate) => IsPrivate = isPrivate;

    public void SetCoverImage(string? url) => CoverImage = url;
}
