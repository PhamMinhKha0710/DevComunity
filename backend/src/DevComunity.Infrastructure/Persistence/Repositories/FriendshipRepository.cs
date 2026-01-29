using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Friendship entity
/// </summary>
public class FriendshipRepository : IFriendshipRepository
{
    private readonly DevComunityDbContext _context;

    public FriendshipRepository(DevComunityDbContext context)
    {
        _context = context;
    }

    public async Task<Friendship?> GetByIdAsync(int friendshipId, CancellationToken cancellationToken = default)
    {
        return await _context.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .FirstOrDefaultAsync(f => f.FriendshipId == friendshipId, cancellationToken);
    }

    public async Task<Friendship?> GetFriendshipAsync(int requesterId, int addresseeId, CancellationToken cancellationToken = default)
    {
        return await _context.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .FirstOrDefaultAsync(f => 
                (f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
                (f.RequesterId == addresseeId && f.AddresseeId == requesterId), 
                cancellationToken);
    }

    public async Task<IEnumerable<Friendship>> GetFriendsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => f.Status == FriendshipStatus.Accepted && 
                       (f.RequesterId == userId || f.AddresseeId == userId))
            .OrderByDescending(f => f.RespondedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Friendship>> GetPendingRequestsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => f.Status == FriendshipStatus.Pending && f.AddresseeId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Friendship>> GetSentRequestsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => f.Status == FriendshipStatus.Pending && f.RequesterId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Friendship> AddAsync(Friendship friendship, CancellationToken cancellationToken = default)
    {
        friendship.CreatedAt = DateTime.UtcNow;
        friendship.Status = FriendshipStatus.Pending;
        await _context.Friendships.AddAsync(friendship, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return friendship;
    }

    public async Task UpdateAsync(Friendship friendship, CancellationToken cancellationToken = default)
    {
        _context.Friendships.Update(friendship);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int friendshipId, CancellationToken cancellationToken = default)
    {
        await _context.Friendships
            .Where(f => f.FriendshipId == friendshipId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> AreFriendsAsync(int userId1, int userId2, CancellationToken cancellationToken = default)
    {
        return await _context.Friendships
            .AnyAsync(f => f.Status == FriendshipStatus.Accepted &&
                ((f.RequesterId == userId1 && f.AddresseeId == userId2) ||
                 (f.RequesterId == userId2 && f.AddresseeId == userId1)), 
                cancellationToken);
    }

    public async Task<int> GetFriendsCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Friendships
            .CountAsync(f => f.Status == FriendshipStatus.Accepted &&
                (f.RequesterId == userId || f.AddresseeId == userId), 
                cancellationToken);
    }
}
