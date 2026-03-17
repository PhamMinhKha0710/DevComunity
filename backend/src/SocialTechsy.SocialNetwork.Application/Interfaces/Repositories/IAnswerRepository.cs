using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Answer entity
/// </summary>
public interface IAnswerRepository
{
    Task<Answer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Answer> Items, int TotalCount)> GetByQuestionIdAsync(int questionId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Answer> AddAsync(Answer answer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Answer answer, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AcceptAnswerAsync(int answerId, int questionId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Answer> Items, int TotalCount)> GetByUserIdAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets answer count and has-accepted flag per question id (for list display).
    /// </summary>
    Task<Dictionary<int, (int Count, bool HasAccepted)>> GetAnswerCountsAndAcceptedByQuestionIdsAsync(int[] questionIds, CancellationToken cancellationToken = default);
}
