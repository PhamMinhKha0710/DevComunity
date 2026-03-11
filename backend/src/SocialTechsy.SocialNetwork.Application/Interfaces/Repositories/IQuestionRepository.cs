using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Question entity
/// </summary>
public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
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
    Task IncrementViewCountAsync(int id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Question> Items, int TotalCount)> GetByUserIdAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
}
