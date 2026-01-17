namespace DevComunity.Domain.Entities;

/// <summary>
/// Repository entity - represents a code repository
/// </summary>
public class Repository
{
    public int RepositoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? GiteaRepoId { get; set; }
    public string? CloneUrl { get; set; }
    public string DefaultBranch { get; set; } = "main";
    public bool IsPrivate { get; set; }
    public int StarCount { get; set; }
    public int ForkCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }

    // Foreign keys
    public int OwnerId { get; set; }

    // Navigation properties
    public virtual User Owner { get; set; } = null!;
}
