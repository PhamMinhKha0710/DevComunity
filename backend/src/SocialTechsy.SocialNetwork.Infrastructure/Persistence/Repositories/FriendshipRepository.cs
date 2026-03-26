using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Friendship entity
/// </summary>
public class FriendshipRepository : IFriendshipRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public FriendshipRepository(SocialTechsySocialNetworkDbContext context)
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

    public async Task<(IEnumerable<Friendship> Items, int TotalCount)> GetFriendsPagedAsync(
        int userId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.RequesterId == userId || f.AddresseeId == userId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(f =>
                f.RequesterId == userId
                    ? (f.Addressee.Username.Contains(term) ||
                       (f.Addressee.DisplayName != null && f.Addressee.DisplayName.Contains(term)))
                    : (f.Requester.Username.Contains(term) ||
                       (f.Requester.DisplayName != null && f.Requester.DisplayName.Contains(term))));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(f => f.RespondedAt ?? f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
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
        await _context.Friendships.AddAsync(friendship, cancellationToken);
        return friendship;
    }

    public async Task UpdateAsync(Friendship friendship, CancellationToken cancellationToken = default)
    {
        _context.Friendships.Update(friendship);
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

    public async Task<IEnumerable<FriendDto>> GetSuggestedFriendsAsync(int userId, int limit = 5, CancellationToken cancellationToken = default)
    {
        var existingFriendIds = await _context.Friendships
            .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .ToListAsync(cancellationToken);

        existingFriendIds.Add(userId);

        var currentUserTagIds = await _context.TagPreferences
            .Where(tp => tp.UserId == userId && tp.IsFollowed)
            .Select(tp => tp.TagId)
            .ToListAsync(cancellationToken);

        var suggestions = await _context.Users
            .Where(u => !existingFriendIds.Contains(u.UserId))
            .Select(u => new
            {
                u.UserId,
                u.Username,
                u.DisplayName,
                u.ProfilePicture,
                CommonTags = _context.TagPreferences
                    .Count(tp => currentUserTagIds.Contains(tp.TagId) && tp.UserId == u.UserId && tp.IsFollowed)
            })
            .OrderByDescending(x => x.CommonTags)
            .ThenBy(x => x.UserId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return suggestions.Select(u => new FriendDto
        {
            UserId = u.UserId,
            Username = u.Username,
            DisplayName = u.DisplayName,
            ProfilePicture = u.ProfilePicture,
            FriendsSince = DateTime.UtcNow
        });
    }

    public async Task<NetworkGrowthDto> GetNetworkGrowthAsync(int userId, int days = 28, CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var acceptedFriendships = await _context.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        f.RespondedAt.HasValue &&
                        f.RespondedAt >= startDate &&
                        (f.RequesterId == userId || f.AddresseeId == userId))
            .Select(f => f.RespondedAt!.Value)
            .ToListAsync(cancellationToken);

        var totalConnections = acceptedFriendships.Count;

        var weeksData = new List<int>(4) { 0, 0, 0, 0 };
        var now = DateTime.UtcNow;

        foreach (var date in acceptedFriendships)
        {
            var daysDiff = (now - date).TotalDays;
            if (daysDiff <= 7) weeksData[0]++;
            else if (daysDiff <= 14) weeksData[1]++;
            else if (daysDiff <= 21) weeksData[2]++;
            else weeksData[3]++;
        }

        return new NetworkGrowthDto
        {
            TotalConnections = totalConnections,
            WeeksData = weeksData
        };
    }
}
