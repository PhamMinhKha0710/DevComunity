using DevComunity.Application.Common.DTOs;
using DevComunity.Domain.Entities;

namespace DevComunity.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for Badge entity
/// </summary>
public interface IBadgeRepository
{
    Task<IEnumerable<Badge>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BadgeDto>> GetAllWithCountAsync(CancellationToken cancellationToken = default);
    Task<Badge?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BadgeDto?> GetByIdWithCountAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserBadge>> GetUserBadgesAsync(int userId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<UserBadge> Items, int TotalCount)> GetBadgeUsersAsync(int badgeId, int page, int pageSize, CancellationToken cancellationToken = default);
}
