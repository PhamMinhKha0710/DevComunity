using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;

namespace DevComunity.Application.QueryHandlers.Badges;

/// <summary>
/// Handler for getting all badges
/// </summary>
public class GetBadgesQueryHandler
{
    private readonly IBadgeRepository _badgeRepository;

    public GetBadgesQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<IEnumerable<BadgeDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var badges = await _badgeRepository.GetAllWithCountAsync(cancellationToken);
        return badges;
    }
}

/// <summary>
/// Handler for getting a badge by ID
/// </summary>
public class GetBadgeByIdQueryHandler
{
    private readonly IBadgeRepository _badgeRepository;

    public GetBadgeByIdQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<BadgeDto?> HandleAsync(int badgeId, CancellationToken cancellationToken)
    {
        return await _badgeRepository.GetByIdWithCountAsync(badgeId, cancellationToken);
    }
}

/// <summary>
/// Handler for getting users who earned a badge
/// </summary>
public class GetBadgeUsersQueryHandler
{
    private readonly IBadgeRepository _badgeRepository;

    public GetBadgeUsersQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<PaginatedResponse<UserDto>> HandleAsync(int badgeId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _badgeRepository.GetBadgeUsersAsync(badgeId, page, pageSize, cancellationToken);

        return new PaginatedResponse<UserDto>
        {
            Items = items.Select(ub => new UserDto
            {
                UserId = ub.User?.UserId ?? 0,
                Username = ub.User?.Username ?? "",
                DisplayName = ub.User?.DisplayName,
                ProfilePicture = ub.User?.ProfilePicture,
                ReputationPoints = ub.User?.ReputationPoints ?? 0
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Handler for getting a user's badges
/// </summary>
public class GetUserBadgesQueryHandler
{
    private readonly IBadgeRepository _badgeRepository;

    public GetUserBadgesQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<IEnumerable<UserBadgeDto>> HandleAsync(int userId, CancellationToken cancellationToken)
    {
        var userBadges = await _badgeRepository.GetUserBadgesAsync(userId, cancellationToken);

        return userBadges.Select(ub => new UserBadgeDto
        {
            UserBadgeId = ub.UserBadgeId,
            BadgeId = ub.BadgeId,
            BadgeName = ub.Badge?.Name ?? "",
            BadgeDescription = ub.Badge?.Description,
            BadgeIconUrl = ub.Badge?.IconUrl,
            BadgeType = ub.Badge?.BadgeType ?? "",
            EarnedDate = ub.EarnedDate
        }).ToList();
    }
}
