using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Domain.Entities;

/// <summary>
/// Post entity - represents a user's post (status update)
/// </summary>
public class Post
{
    public int PostId { get; set; }
    public int AuthorId { get; set; }
    public int? GroupId { get; set; }  // null = public post, not group post
    public string Content { get; set; } = null!;
    public string? MediaUrls { get; set; } // JSON-encoded array of URLs
    public PostVisibility Visibility { get; set; } = PostVisibility.Public; // Default to Public
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User Author { get; set; } = null!;
    public virtual Group? Group { get; set; }
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<SavedItem> SavedByUsers { get; set; } = new List<SavedItem>();
}
