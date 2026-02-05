namespace SocialTechsy.SocialNetwork.Infrastructure.External.Gitea;

/// <summary>
/// Service interface for Gitea API operations
/// </summary>
public interface IGiteaService
{
    /// <summary>
    /// Get the tree structure of a repository at a specific commit/branch
    /// </summary>
    /// <param name="owner">Repository owner username</param>
    /// <param name="repo">Repository name</param>
    /// <param name="sha">Commit SHA or branch name (e.g., "main", "HEAD")</param>
    /// <param name="recursive">Whether to include subdirectories recursively</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tree response or null if not found</returns>
    Task<GiteaTreeResponse?> GetRepositoryTreeAsync(
        string owner, 
        string repo, 
        string sha = "HEAD",
        bool recursive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the contents of a file or directory
    /// </summary>
    /// <param name="owner">Repository owner username</param>
    /// <param name="repo">Repository name</param>
    /// <param name="path">File or directory path within the repository</param>
    /// <param name="branch">Branch or commit reference</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Content response or null if not found</returns>
    Task<GiteaContentResponse?> GetFileContentAsync(
        string owner, 
        string repo, 
        string path,
        string? branch = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get directory contents (list of files/subdirectories)
    /// </summary>
    /// <param name="owner">Repository owner username</param>
    /// <param name="repo">Repository name</param>
    /// <param name="path">Directory path (empty for root)</param>
    /// <param name="branch">Branch or commit reference</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of content entries</returns>
    Task<List<GiteaContentResponse>> GetDirectoryContentsAsync(
        string owner, 
        string repo, 
        string? path = null,
        string? branch = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get commit history for a repository
    /// </summary>
    /// <param name="owner">Repository owner username</param>
    /// <param name="repo">Repository name</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of commits per page</param>
    /// <param name="sha">Branch or commit to start from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of commits</returns>
    Task<List<GiteaCommitResponse>> GetCommitsAsync(
        string owner, 
        string repo, 
        int page = 1, 
        int pageSize = 20,
        string? sha = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single commit by SHA
    /// </summary>
    /// <param name="owner">Repository owner username</param>
    /// <param name="repo">Repository name</param>
    /// <param name="sha">Commit SHA</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Commit details or null if not found</returns>
    Task<GiteaCommitResponse?> GetCommitAsync(
        string owner, 
        string repo, 
        string sha,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if Gitea service is available and configured
    /// </summary>
    bool IsConfigured { get; }
}
