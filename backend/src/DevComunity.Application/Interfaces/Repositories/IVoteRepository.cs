using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Vote entity
/// </summary>
public interface IVoteRepository
{
    Task<Vote?> GetUserVoteAsync(int userId, int? questionId, int? answerId, CancellationToken cancellationToken = default);
    Task<Vote?> GetUserVoteOnQuestionAsync(int userId, int questionId, CancellationToken cancellationToken = default);
    Task<Vote?> GetUserVoteOnAnswerAsync(int userId, int answerId, CancellationToken cancellationToken = default);
    Task<Vote> AddAsync(Vote vote, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vote vote, CancellationToken cancellationToken = default);
    Task DeleteAsync(int voteId, CancellationToken cancellationToken = default);
    Task<int> GetScoreAsync(int? questionId, int? answerId, CancellationToken cancellationToken = default);
    Task<int> GetQuestionScoreAsync(int questionId, CancellationToken cancellationToken = default);
    Task<int> GetAnswerScoreAsync(int answerId, CancellationToken cancellationToken = default);
}

