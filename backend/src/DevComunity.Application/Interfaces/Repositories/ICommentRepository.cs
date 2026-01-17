using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Comment entity
/// </summary>
public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Comment>> GetByQuestionIdAsync(int questionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Comment>> GetByAnswerIdAsync(int answerId, CancellationToken cancellationToken = default);
    Task<Comment> AddAsync(Comment comment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
