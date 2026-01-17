using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Tag entity
/// </summary>
public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Tag?> GetByNameAsync(string tagName, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Tag> Items, int TotalCount)> GetPaginatedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        string sort = "popular",
        CancellationToken cancellationToken = default);
    Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default);
    Task<Tag> GetOrCreateAsync(string tagName, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tag>> GetPopularTagsAsync(int count = 10, CancellationToken cancellationToken = default);
}
