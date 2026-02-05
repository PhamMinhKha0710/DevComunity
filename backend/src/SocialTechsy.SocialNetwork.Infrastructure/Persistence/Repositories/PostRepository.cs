using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Post entity
/// </summary>
public class PostRepository : IPostRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public PostRepository(SocialTechsySocialNetworkDbContext context)
    {
        _context = context;
    }

    public async Task<Post?> GetByIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Group)
            .FirstOrDefaultAsync(p => p.PostId == postId, cancellationToken);
    }

    public async Task<(IEnumerable<Post> Items, int TotalCount)> GetNewsfeedAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Get friends list (users who are friends with current user)
        var friendIds = await _context.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                (f.RequesterId == userId || f.AddresseeId == userId))
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Get followed users
        var followingIds = await _context.UserFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync(cancellationToken);

        // Get user's groups
        var userGroups = await _context.GroupMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId)
            .ToListAsync(cancellationToken);

        // Combine all user IDs for newsfeed
        var relevantUserIds = friendIds.Union(followingIds).Distinct().ToList();
        relevantUserIds.Add(userId); // Include own posts

        // Query posts from friends, followed users, and user's groups
        var query = _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Group)
            .Where(p => 
                // Public posts from friends and followed users
                (p.GroupId == null && relevantUserIds.Contains(p.AuthorId)) ||
                // Group posts from user's groups
                (p.GroupId != null && userGroups.Contains(p.GroupId.Value)));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Post> Items, int TotalCount)> GetGroupPostsAsync(
        int groupId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Posts
            .Include(p => p.Author)
            .Where(p => p.GroupId == groupId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Post> Items, int TotalCount)> GetUserPostsAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Group)
            .Where(p => p.AuthorId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Post> AddAsync(Post post, CancellationToken cancellationToken = default)
    {
        post.CreatedAt = DateTime.UtcNow;
        await _context.Posts.AddAsync(post, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return post;
    }

    public async Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
    {
        post.UpdatedAt = DateTime.UtcNow;
        _context.Posts.Update(post);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int postId, CancellationToken cancellationToken = default)
    {
        await _context.Posts
            .Where(p => p.PostId == postId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
