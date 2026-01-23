using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Badge entity
/// </summary>
public class BadgeRepository : IBadgeRepository
{
    private readonly DevComunityDbContext _context;

    public BadgeRepository(DevComunityDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Badge>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Badges.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BadgeDto>> GetAllWithCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Badges
            .Select(b => new BadgeDto
            {
                BadgeId = b.BadgeId,
                Name = b.Name,
                Description = b.Description,
                IconUrl = b.IconUrl,
                BadgeType = b.BadgeType,
                RequiredPoints = b.RequiredPoints,
                EarnedByCount = _context.UserBadges.Count(ub => ub.BadgeId == b.BadgeId)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Badge?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Badges.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<BadgeDto?> GetByIdWithCountAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Badges
            .Where(b => b.BadgeId == id)
            .Select(b => new BadgeDto
            {
                BadgeId = b.BadgeId,
                Name = b.Name,
                Description = b.Description,
                IconUrl = b.IconUrl,
                BadgeType = b.BadgeType,
                RequiredPoints = b.RequiredPoints,
                EarnedByCount = _context.UserBadges.Count(ub => ub.BadgeId == b.BadgeId)
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserBadge>> GetUserBadgesAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserBadges
            .Include(ub => ub.Badge)
            .Where(ub => ub.UserId == userId)
            .OrderByDescending(ub => ub.EarnedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<UserBadge> Items, int TotalCount)> GetBadgeUsersAsync(
        int badgeId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.UserBadges
            .Include(ub => ub.User)
            .Where(ub => ub.BadgeId == badgeId)
            .OrderByDescending(ub => ub.EarnedDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
