using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Notification entity
/// </summary>
public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Notification> Items, int TotalCount)> GetByUserIdAsync(
        int userId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
