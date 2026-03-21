using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.External;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Repositories;

public class GetRepositoriesQuery : IRequest<PaginatedResponse<RepositoryDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public string? Search { get; set; }
}

public class GetRepositoryByIdQuery : IRequest<RepositoryDto?>
{
    public int RepositoryId { get; set; }
}

public class GetUserRepositoriesQuery : IRequest<IEnumerable<RepositoryDto>>
{
    public int UserId { get; set; }
}

/// <summary>
/// Handler for getting paginated repositories
/// </summary>
public class GetRepositoriesQueryHandler : IRequestHandler<GetRepositoriesQuery, PaginatedResponse<RepositoryDto>>
{
    private readonly ICodeRepository _codeRepository;

    public GetRepositoriesQueryHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<PaginatedResponse<RepositoryDto>> Handle(
        GetRepositoriesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _codeRepository.GetPaginatedAsync(request.Page, request.PageSize, request.Search, null, cancellationToken);

        return new PaginatedResponse<RepositoryDto>
        {
            Items = items.Select(r => new RepositoryDto
            {
                RepositoryId = r.RepositoryId,
                Name = r.Name,
                Description = r.Description,
                CloneUrl = r.CloneUrl,
                DefaultBranch = r.DefaultBranch,
                IsPrivate = r.IsPrivate,
                StarCount = r.StarCount,
                ForkCount = r.ForkCount,
                CreatedDate = r.CreatedDate,
                LastUpdatedDate = r.LastUpdatedDate,
                OwnerId = r.OwnerId,
                OwnerUsername = r.Owner?.Username,
                OwnerProfilePicture = r.Owner?.ProfilePicture
            }).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Handler for getting a repository by ID
/// </summary>
public class GetRepositoryByIdQueryHandler : IRequestHandler<GetRepositoryByIdQuery, RepositoryDto?>
{
    private readonly ICodeRepository _codeRepository;

    public GetRepositoryByIdQueryHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<RepositoryDto?> Handle(GetRepositoryByIdQuery request, CancellationToken cancellationToken)
    {
        var r = await _codeRepository.GetByIdAsync(request.RepositoryId, cancellationToken);
        if (r == null) return null;

        return new RepositoryDto
        {
            RepositoryId = r.RepositoryId,
            Name = r.Name,
            Description = r.Description,
            CloneUrl = r.CloneUrl,
            DefaultBranch = r.DefaultBranch,
            IsPrivate = r.IsPrivate,
            StarCount = r.StarCount,
            ForkCount = r.ForkCount,
            CreatedDate = r.CreatedDate,
            LastUpdatedDate = r.LastUpdatedDate,
            OwnerId = r.OwnerId,
            OwnerUsername = r.Owner?.Username,
            OwnerProfilePicture = r.Owner?.ProfilePicture
        };
    }
}

/// <summary>
/// Handler for getting user's repositories
/// </summary>
public class GetUserRepositoriesQueryHandler : IRequestHandler<GetUserRepositoriesQuery, IEnumerable<RepositoryDto>>
{
    private readonly ICodeRepository _codeRepository;

    public GetUserRepositoriesQueryHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<IEnumerable<RepositoryDto>> Handle(GetUserRepositoriesQuery request, CancellationToken cancellationToken)
    {
        var items = await _codeRepository.GetByOwnerIdAsync(request.UserId, cancellationToken);

        return items.Select(r => new RepositoryDto
        {
            RepositoryId = r.RepositoryId,
            Name = r.Name,
            Description = r.Description,
            CloneUrl = r.CloneUrl,
            DefaultBranch = r.DefaultBranch,
            IsPrivate = r.IsPrivate,
            StarCount = r.StarCount,
            ForkCount = r.ForkCount,
            CreatedDate = r.CreatedDate,
            LastUpdatedDate = r.LastUpdatedDate,
            OwnerId = r.OwnerId,
            OwnerUsername = r.Owner?.Username,
            OwnerProfilePicture = r.Owner?.ProfilePicture
        }).ToList();
    }
}
