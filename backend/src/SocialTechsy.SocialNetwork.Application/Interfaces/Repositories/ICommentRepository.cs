using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Comment entity
/// </summary>
public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Comment>> GetByQuestionIdAsync(int questionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Comment>> GetByAnswerIdAsync(int answerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Comment>> GetByPostIdAsync(int postId, CancellationToken cancellationToken = default);
    Task<int> GetCountByPostIdAsync(int postId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetCountsByPostIdsAsync(IEnumerable<int> postIds, CancellationToken cancellationToken = default);
    Task<Comment> AddAsync(Comment comment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
