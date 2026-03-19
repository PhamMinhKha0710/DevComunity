using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure.External.Gitea;

/// <summary>
/// Adapter that wraps IGiteaService and exposes IExternalGitService for the Application layer.
/// This keeps the Infrastructure (Gitea-specific) types contained within the Infrastructure project.
/// </summary>
public class ExternalGitServiceAdapter : IExternalGitService
{
    private readonly IGiteaService _giteaService;

    public ExternalGitServiceAdapter(IGiteaService giteaService)
    {
        _giteaService = giteaService;
    }

    public bool IsConfigured => _giteaService.IsConfigured;

    public async Task<List<GitContentResponse>> GetDirectoryContentsAsync(
        string owner, string repo, string? path = null, string? branch = null, CancellationToken cancellationToken = default)
    {
        var results = await _giteaService.GetDirectoryContentsAsync(owner, repo, path, branch, cancellationToken);
        return results.Select(r => new GitContentResponse
        {
            Name = r.Name,
            Path = r.Path,
            Type = r.Type,
            Size = r.Size,
            Sha = r.Sha,
            Content = r.Content,
            Encoding = r.Encoding
        }).ToList();
    }

    public async Task<GitContentResponse?> GetFileContentAsync(
        string owner, string repo, string path, string? branch = null, CancellationToken cancellationToken = default)
    {
        var result = await _giteaService.GetFileContentAsync(owner, repo, path, branch, cancellationToken);
        if (result == null) return null;

        return new GitContentResponse
        {
            Name = result.Name,
            Path = result.Path,
            Type = result.Type,
            Size = result.Size,
            Sha = result.Sha,
            Content = result.Content,
            Encoding = result.Encoding
        };
    }

    public async Task<List<GitCommitResponse>> GetCommitsAsync(
        string owner, string repo, int page = 1, int pageSize = 20, string? sha = null, CancellationToken cancellationToken = default)
    {
        var results = await _giteaService.GetCommitsAsync(owner, repo, page, pageSize, sha, cancellationToken);
        return results.Select(r => new GitCommitResponse
        {
            Sha = r.Sha,
            Commit = new GitCommitDetail
            {
                Message = r.Commit.Message,
                Author = new GitCommitAuthor
                {
                    Name = r.Commit.Author.Name,
                    Email = r.Commit.Author.Email,
                    Date = r.Commit.Author.Date
                },
                Committer = new GitCommitAuthor
                {
                    Name = r.Commit.Committer.Name,
                    Email = r.Commit.Committer.Email,
                    Date = r.Commit.Committer.Date
                }
            },
            Author = r.Author != null ? new GitAuthor { AvatarUrl = r.Author.AvatarUrl } : null,
            HtmlUrl = r.HtmlUrl
        }).ToList();
    }
}
