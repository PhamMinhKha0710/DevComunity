using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Enums;
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
        int userId, int page, int pageSize, string? filter = null, CancellationToken cancellationToken = default)
    {
        // Use subqueries instead of loading IDs into memory to avoid large IN clauses
        var friendIdsQuery = _context.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                (f.RequesterId == userId || f.AddresseeId == userId))
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId);

        var followingIdsQuery = _context.UserFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId);

        var userGroupsQuery = _context.GroupMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId);

        var query = _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Group)
            .AsQueryable();

        if (filter?.ToLower() == "following")
        {
            query = query.Where(p => followingIdsQuery.Contains(p.AuthorId));
        }
        else if (filter?.ToLower() == "friends")
        {
            query = query.Where(p => friendIdsQuery.Contains(p.AuthorId));
        }
        else if (filter?.ToLower() == "groups")
        {
            query = query.Where(p => p.GroupId != null && userGroupsQuery.Contains(p.GroupId.Value));
        }
        else // default: foryou / all
        {
            query = query.Where(p =>
                (p.GroupId == null && (
                    p.AuthorId == userId ||
                    friendIdsQuery.Contains(p.AuthorId) ||
                    followingIdsQuery.Contains(p.AuthorId) ||
                    p.Visibility == PostVisibility.Public)) ||
                (p.GroupId != null && userGroupsQuery.Contains(p.GroupId.Value)));
        }

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
            .Include(p => p.Group)
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
        return post;
    }

    public async Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
    {
        post.UpdatedAt = DateTime.UtcNow;
        _context.Posts.Update(post);
    }

    public async Task DeleteAsync(int postId, CancellationToken cancellationToken = default)
    {
        await _context.Posts
            .Where(p => p.PostId == postId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
