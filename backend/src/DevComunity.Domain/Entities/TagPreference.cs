using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevComunity.Domain.Entities;

/// <summary>
/// User's preference for a tag - follow or ignore
/// </summary>
public class TagPreference
{
    [Key]
    public int TagPreferenceId { get; set; }
    
    public int UserId { get; set; }
    
    public int TagId { get; set; }
    
    /// <summary>
    /// If true, the user follows this tag and wants to see related questions
    /// </summary>
    public bool IsFollowed { get; set; }
    
    /// <summary>
    /// If true, the user wants to hide questions with this tag
    /// </summary>
    public bool IsIgnored { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
    
    [ForeignKey("TagId")]
    public virtual Tag? Tag { get; set; }
}
