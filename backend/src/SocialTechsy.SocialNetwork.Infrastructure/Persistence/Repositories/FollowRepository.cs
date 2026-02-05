using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for UserFollow entity
/// </summary>
public class FollowRepository : IFollowRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public FollowRepository(SocialTechsySocialNetworkDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsFollowingAsync(int followerId, int followingId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId, cancellationToken);
    }

    public async Task<IEnumerable<UserFollow>> GetFollowersAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .Include(f => f.Follower)
            .Where(f => f.FollowingId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserFollow>> GetFollowingAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .Include(f => f.Following)
            .Where(f => f.FollowerId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserFollow> FollowAsync(UserFollow follow, CancellationToken cancellationToken = default)
    {
        follow.CreatedAt = DateTime.UtcNow;
        await _context.UserFollows.AddAsync(follow, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return follow;
    }

    public async Task UnfollowAsync(int followerId, int followingId, CancellationToken cancellationToken = default)
    {
        await _context.UserFollows
            .Where(f => f.FollowerId == followerId && f.FollowingId == followingId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> GetFollowersCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .CountAsync(f => f.FollowingId == userId, cancellationToken);
    }

    public async Task<int> GetFollowingCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .CountAsync(f => f.FollowerId == userId, cancellationToken);
    }
}
