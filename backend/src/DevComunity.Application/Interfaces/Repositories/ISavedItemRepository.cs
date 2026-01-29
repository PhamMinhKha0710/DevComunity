using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for SavedItem entity
/// </summary>
public interface ISavedItemRepository
{
    Task<SavedItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SavedItem>> GetByUserIdAsync(int userId, string? type = null, int page = 1, int pageSize = 15, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(int userId, string? type = null, CancellationToken cancellationToken = default);
    Task<SavedItem?> FindAsync(int userId, int? questionId, int? answerId, CancellationToken cancellationToken = default);
    Task<bool> IsSavedAsync(int userId, int? questionId, int? answerId, CancellationToken cancellationToken = default);
    Task<SavedItem> AddAsync(SavedItem savedItem, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteByQuestionAsync(int userId, int questionId, CancellationToken cancellationToken = default);
    Task DeleteByAnswerAsync(int userId, int answerId, CancellationToken cancellationToken = default);
}
