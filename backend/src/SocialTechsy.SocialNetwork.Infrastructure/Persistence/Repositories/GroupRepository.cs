using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Group and GroupMember entities
/// </summary>
public class GroupRepository : IGroupRepository
{
    private readonly SocialTechsySocialNetworkDbContext _context;

    public GroupRepository(SocialTechsySocialNetworkDbContext context)
    {
        _context = context;
    }

    // ============ Group CRUD ============

    public async Task<Group?> GetByIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await _context.Groups
            .Include(g => g.Creator)
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.GroupId == groupId, cancellationToken);
    }

    public async Task<(IEnumerable<Group> Items, int TotalCount)> GetPaginatedAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Groups
            .Include(g => g.Creator)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g => g.Name.Contains(search) || 
                (g.Description != null && g.Description.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<Group>> GetUserGroupsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Groups
            .Include(g => g.Creator)
            .Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        group.CreatedAt = DateTime.UtcNow;
        await _context.Groups.AddAsync(group, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return group;
    }

    public async Task UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        _context.Groups.Update(group);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int groupId, CancellationToken cancellationToken = default)
    {
        // Delete members first, then group
        await _context.GroupMembers.Where(m => m.GroupId == groupId).ExecuteDeleteAsync(cancellationToken);
        await _context.Groups.Where(g => g.GroupId == groupId).ExecuteDeleteAsync(cancellationToken);
    }

    // ============ Member Operations ============

    public async Task<GroupMember?> GetMemberAsync(int groupId, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<GroupMember>> GetMembersAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupMembers
            .Include(m => m.User)
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupMember> AddMemberAsync(GroupMember member, CancellationToken cancellationToken = default)
    {
        member.JoinedAt = DateTime.UtcNow;
        await _context.GroupMembers.AddAsync(member, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return member;
    }

    public async Task RemoveMemberAsync(int groupId, int userId, CancellationToken cancellationToken = default)
    {
        await _context.GroupMembers
            .Where(m => m.GroupId == groupId && m.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task UpdateMemberRoleAsync(int groupId, int userId, GroupRole role, CancellationToken cancellationToken = default)
    {
        await _context.GroupMembers
            .Where(m => m.GroupId == groupId && m.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Role, role), cancellationToken);
    }

    public async Task<bool> IsMemberAsync(int groupId, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);
    }

    public async Task<int> GetMemberCountAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupMembers
            .CountAsync(m => m.GroupId == groupId, cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetMemberCountsBatchAsync(IEnumerable<int> groupIds, CancellationToken cancellationToken = default)
    {
        var ids = groupIds.ToList();
        return await _context.GroupMembers
            .Where(m => ids.Contains(m.GroupId))
            .GroupBy(m => m.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<int, GroupMember?>> GetUserMembershipsBatchAsync(IEnumerable<int> groupIds, int userId, CancellationToken cancellationToken = default)
    {
        var ids = groupIds.ToList();
        var memberships = await _context.GroupMembers
            .Where(m => ids.Contains(m.GroupId) && m.UserId == userId)
            .ToDictionaryAsync(m => m.GroupId, cancellationToken);

        return ids.ToDictionary(id => id, id => memberships.GetValueOrDefault(id));
    }
}
