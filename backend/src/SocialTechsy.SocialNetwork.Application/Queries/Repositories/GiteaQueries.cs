using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.External;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;

namespace SocialTechsy.SocialNetwork.Application.Queries.Repositories;

/// <summary>
/// Query for getting repository file directory contents
/// </summary>
public class GetRepositoryFilesQuery : IRequest<List<RepositoryFileDto>>
{
    public int RepositoryId { get; set; }
    public string? Path { get; set; }
    public string Branch { get; set; } = "main";
}

/// <summary>
/// Query for getting a single file's content
/// </summary>
public class GetFileContentQuery : IRequest<FileContentDto?>
{
    public int RepositoryId { get; set; }
    public string FilePath { get; set; } = null!;
    public string Branch { get; set; } = "main";
}

/// <summary>
/// Query for getting repository commit history
/// </summary>
public class GetCommitsQuery : IRequest<PaginatedResponse<CommitDto>>
{
    public int RepositoryId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Branch { get; set; }
}
