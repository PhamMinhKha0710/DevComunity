using MediatR;
using System;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Queries.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Gitea;

public class GetRepositoryFilesQueryHandler : IRequestHandler<GetRepositoryFilesQuery, List<RepositoryFileDto>>
{
    private readonly IExternalGitService _giteaService;
    private readonly ICodeRepository _codeRepository;

    public GetRepositoryFilesQueryHandler(IExternalGitService giteaService, ICodeRepository codeRepository)
    {
        _giteaService = giteaService;
        _codeRepository = codeRepository;
    }

    public async Task<List<RepositoryFileDto>> Handle(GetRepositoryFilesQuery request, CancellationToken cancellationToken)
    {
        var repository = await _codeRepository.GetByIdAsync(request.RepositoryId, cancellationToken);
        if (repository == null)
            throw new InvalidOperationException($"Repository with ID {request.RepositoryId} not found");

        var ownerUsername = repository.Owner?.Username ?? repository.OwnerId.ToString();
        var contents = await _giteaService.GetDirectoryContentsAsync(
            ownerUsername,
            repository.Name,
            request.Path,
            request.Branch,
            cancellationToken);

        return contents.Select(c => new RepositoryFileDto
        {
            Name = c.Name,
            Path = c.Path,
            Type = c.Type == "dir" ? "tree" : "blob",
            Size = c.Size,
            Sha = c.Sha
        }).ToList();
    }
}

public class GetFileContentQueryHandler : IRequestHandler<GetFileContentQuery, FileContentDto?>
{
    private readonly IExternalGitService _giteaService;
    private readonly ICodeRepository _codeRepository;

    public GetFileContentQueryHandler(IExternalGitService giteaService, ICodeRepository codeRepository)
    {
        _giteaService = giteaService;
        _codeRepository = codeRepository;
    }

    public async Task<FileContentDto?> Handle(GetFileContentQuery request, CancellationToken cancellationToken)
    {
        var repository = await _codeRepository.GetByIdAsync(request.RepositoryId, cancellationToken);
        if (repository == null)
            return null;

        var ownerUsername = repository.Owner?.Username ?? repository.OwnerId.ToString();
        var content = await _giteaService.GetFileContentAsync(
            ownerUsername,
            repository.Name,
            request.FilePath,
            request.Branch,
            cancellationToken);

        if (content == null)
            return null;

        var decodedContent = "";
        if (!string.IsNullOrEmpty(content.Content) && content.Encoding == "base64")
        {
            try
            {
                var bytes = Convert.FromBase64String(content.Content);
                decodedContent = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                decodedContent = content.Content;
            }
        }
        else
        {
            decodedContent = content.Content ?? "";
        }

        return new FileContentDto
        {
            Path = content.Path,
            Content = decodedContent,
            Encoding = "utf-8",
            Size = content.Size ?? 0,
            Sha = content.Sha
        };
    }
}

public class GetCommitsQueryHandler : IRequestHandler<GetCommitsQuery, PaginatedResponse<CommitDto>>
{
    private readonly IExternalGitService _giteaService;
    private readonly ICodeRepository _codeRepository;

    public GetCommitsQueryHandler(IExternalGitService giteaService, ICodeRepository codeRepository)
    {
        _giteaService = giteaService;
        _codeRepository = codeRepository;
    }

    public async Task<PaginatedResponse<CommitDto>> Handle(GetCommitsQuery request, CancellationToken cancellationToken)
    {
        var repository = await _codeRepository.GetByIdAsync(request.RepositoryId, cancellationToken);
        if (repository == null)
            throw new InvalidOperationException($"Repository with ID {request.RepositoryId} not found");

        var ownerUsername = repository.Owner?.Username ?? repository.OwnerId.ToString();
        var giteaCommits = await _giteaService.GetCommitsAsync(
            ownerUsername,
            repository.Name,
            request.Page,
            request.PageSize,
            request.Branch,
            cancellationToken);

        var commits = giteaCommits.Select(c => new CommitDto
        {
            Sha = c.Sha,
            Message = c.Commit.Message,
            AuthorName = c.Commit.Author.Name,
            AuthorEmail = c.Commit.Author.Email,
            AuthorAvatar = c.Author?.AvatarUrl,
            CommittedAt = c.Commit.Author.Date,
            Url = c.HtmlUrl
        }).ToList();

        return new PaginatedResponse<CommitDto>
        {
            Items = commits,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = commits.Count
        };
    }
}
