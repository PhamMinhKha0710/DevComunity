using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Notification entity
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly DevComunityDbContext _context;

    public NotificationRepository(DevComunityDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == id, cancellationToken);
    }

    public async Task<(IEnumerable<Notification> Items, int TotalCount)> GetByUserIdAsync(
        int userId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        query = query.OrderByDescending(n => n.CreatedDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public async Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications.FindAsync(new object[] { id }, cancellationToken);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications.FindAsync(new object[] { id }, cancellationToken);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
