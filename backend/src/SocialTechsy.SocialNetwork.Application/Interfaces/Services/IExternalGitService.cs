namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

/// <summary>
/// Abstraction for external Git service operations (e.g. Gitea)
/// </summary>
public interface IExternalGitService
{
    Task<List<GitContentResponse>> GetDirectoryContentsAsync(
        string owner, string repo, string? path = null, string? branch = null, CancellationToken cancellationToken = default);

    Task<GitContentResponse?> GetFileContentAsync(
        string owner, string repo, string path, string? branch = null, CancellationToken cancellationToken = default);

    Task<List<GitCommitResponse>> GetCommitsAsync(
        string owner, string repo, int page = 1, int pageSize = 20, string? sha = null, CancellationToken cancellationToken = default);

    bool IsConfigured { get; }
}

/// <summary>
/// Response model for a file or directory entry
/// </summary>
public class GitContentResponse
{
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;
    public string Type { get; set; } = null!;
    public long? Size { get; set; }
    public string? Sha { get; set; }
    public string? Content { get; set; }
    public string? Encoding { get; set; }
}

/// <summary>
/// Response model for a commit
/// </summary>
public class GitCommitResponse
{
    public string Sha { get; set; } = null!;
    public GitCommitDetail Commit { get; set; } = null!;
    public GitAuthor? Author { get; set; }
    public string? HtmlUrl { get; set; }
}

/// <summary>
/// Commit detail (message, author info)
/// </summary>
public class GitCommitDetail
{
    public GitCommitAuthor Author { get; set; } = null!;
    public GitCommitAuthor Committer { get; set; } = null!;
    public string Message { get; set; } = null!;
}

/// <summary>
/// Author info inside a commit
/// </summary>
public class GitCommitAuthor
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime Date { get; set; }
}

/// <summary>
/// Git author (avatar, etc.)
/// </summary>
public class GitAuthor
{
    public string? AvatarUrl { get; set; }
}
