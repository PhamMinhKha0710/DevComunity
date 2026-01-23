using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;

namespace DevComunity.Application.QueryHandlers.Repositories;

/// <summary>
/// Handler for getting paginated repositories
/// </summary>
public class GetRepositoriesQueryHandler
{
    private readonly ICodeRepository _codeRepository;

    public GetRepositoriesQueryHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<PaginatedResponse<RepositoryDto>> HandleAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _codeRepository.GetPaginatedAsync(page, pageSize, search, null, cancellationToken);

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
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Handler for getting a repository by ID
/// </summary>
public class GetRepositoryByIdQueryHandler
{
    private readonly ICodeRepository _codeRepository;

    public GetRepositoryByIdQueryHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<RepositoryDto?> HandleAsync(int repositoryId, CancellationToken cancellationToken)
    {
        var r = await _codeRepository.GetByIdAsync(repositoryId, cancellationToken);
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
public class GetUserRepositoriesQueryHandler
{
    private readonly ICodeRepository _codeRepository;

    public GetUserRepositoriesQueryHandler(ICodeRepository codeRepository)
    {
        _codeRepository = codeRepository;
    }

    public async Task<IEnumerable<RepositoryDto>> HandleAsync(int userId, CancellationToken cancellationToken)
    {
        var items = await _codeRepository.GetByOwnerIdAsync(userId, cancellationToken);

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
