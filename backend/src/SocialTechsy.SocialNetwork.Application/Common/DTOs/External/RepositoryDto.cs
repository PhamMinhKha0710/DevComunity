namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.External;

/// <summary>
/// DTO for repository response
/// </summary>
public class RepositoryDto
{
    public int RepositoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? CloneUrl { get; set; }
    public string DefaultBranch { get; set; } = "main";
    public bool IsPrivate { get; set; }
    public int StarCount { get; set; }
    public int ForkCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    
    // Owner info
    public int OwnerId { get; set; }
    public string? OwnerUsername { get; set; }
    public string? OwnerProfilePicture { get; set; }
}

/// <summary>
/// DTO for repository file/tree item
/// </summary>
public class RepositoryFileDto
{
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;
    public string Type { get; set; } = null!; // file, dir
    public long? Size { get; set; }
    public string? Sha { get; set; }
}

/// <summary>
/// DTO for repository file content
/// </summary>
public class FileContentDto
{
    public string Path { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string Encoding { get; set; } = "utf-8";
    public long Size { get; set; }
    public string? Sha { get; set; }
}

/// <summary>
/// DTO for commit
/// </summary>
public class CommitDto
{
    public string Sha { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string AuthorName { get; set; } = null!;
    public string? AuthorEmail { get; set; }
    public string? AuthorAvatar { get; set; }
    public DateTime CommittedAt { get; set; }
    public string? Url { get; set; }
}

