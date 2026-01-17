using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Question entity
/// </summary>
public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Question>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<Question> Items, int TotalCount)> GetPaginatedAsync(
        int page, 
        int pageSize, 
        string? searchTerm = null, 
        string? tag = null, 
        string sort = "newest",
        CancellationToken cancellationToken = default);
    Task<Question> AddAsync(Question question, CancellationToken cancellationToken = default);
    Task UpdateAsync(Question question, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
