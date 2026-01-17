using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Answer entity
/// </summary>
public interface IAnswerRepository
{
    Task<Answer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId, CancellationToken cancellationToken = default);
    Task<Answer> AddAsync(Answer answer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Answer answer, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AcceptAnswerAsync(int answerId, int questionId, CancellationToken cancellationToken = default);
}
