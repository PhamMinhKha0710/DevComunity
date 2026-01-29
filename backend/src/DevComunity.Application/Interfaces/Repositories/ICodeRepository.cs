using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Code Repository entity (not to confuse with pattern name)
/// </summary>
public interface ICodeRepository
{
    Task<(IEnumerable<Repository> Items, int TotalCount)> GetPaginatedAsync(
        int page, int pageSize, string? search = null, int? ownerId = null, CancellationToken cancellationToken = default);
    Task<Repository?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Repository>> GetByOwnerIdAsync(int ownerId, CancellationToken cancellationToken = default);
    Task<Repository> AddAsync(Repository repository, CancellationToken cancellationToken = default);
    Task UpdateAsync(Repository repository, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
