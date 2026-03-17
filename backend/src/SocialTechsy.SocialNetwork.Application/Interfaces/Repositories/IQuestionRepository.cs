using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Question entity
/// </summary>
public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a question by ID for update operations. Does NOT include navigation properties
    /// (User, Answers, QuestionTags, Comments) to avoid EF Core tracking conflicts when
    /// updating the question and its tags in the same request.
    /// </summary>
    Task<Question?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);
    
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
    Task IncrementViewCountByDeltaAsync(int id, long delta, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Question> Items, int TotalCount)> GetByUserIdAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all tags for a question (creates tags by name if they do not exist).
    /// </summary>
    Task SetTagsForQuestionAsync(int questionId, IReadOnlyList<string> tagNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated questions with optimized query (no tracking, minimal includes).
    /// </summary>
    Task<(IEnumerable<Question> Items, int TotalCount)> GetPaginatedOptimizedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        string? tag = null,
        string sort = "newest",
        CancellationToken cancellationToken = default);
}
