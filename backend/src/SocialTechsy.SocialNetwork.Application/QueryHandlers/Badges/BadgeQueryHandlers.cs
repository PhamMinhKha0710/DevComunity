using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.External;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Badges;

public class GetBadgesQuery : IRequest<IEnumerable<BadgeDto>> { }

public class GetBadgeByIdQuery : IRequest<BadgeDto?>
{
    public int BadgeId { get; set; }
}

public class GetBadgeUsersQuery : IRequest<PaginatedResponse<UserDto>>
{
    public int BadgeId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetUserBadgesQuery : IRequest<IEnumerable<UserBadgeDto>>
{
    public int UserId { get; set; }
}

/// <summary>
/// Handler for getting all badges
/// </summary>
public class GetBadgesQueryHandler : IRequestHandler<GetBadgesQuery, IEnumerable<BadgeDto>>
{
    private readonly IBadgeRepository _badgeRepository;

    public GetBadgesQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<IEnumerable<BadgeDto>> Handle(GetBadgesQuery request, CancellationToken cancellationToken)
    {
        var badges = await _badgeRepository.GetAllWithCountAsync(cancellationToken);
        return badges;
    }
}

/// <summary>
/// Handler for getting a badge by ID
/// </summary>
public class GetBadgeByIdQueryHandler : IRequestHandler<GetBadgeByIdQuery, BadgeDto?>
{
    private readonly IBadgeRepository _badgeRepository;

    public GetBadgeByIdQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<BadgeDto?> Handle(GetBadgeByIdQuery request, CancellationToken cancellationToken)
    {
        return await _badgeRepository.GetByIdWithCountAsync(request.BadgeId, cancellationToken);
    }
}

/// <summary>
/// Handler for getting users who earned a badge
/// </summary>
public class GetBadgeUsersQueryHandler : IRequestHandler<GetBadgeUsersQuery, PaginatedResponse<UserDto>>
{
    private readonly IBadgeRepository _badgeRepository;

    public GetBadgeUsersQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<PaginatedResponse<UserDto>> Handle(GetBadgeUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _badgeRepository.GetBadgeUsersAsync(request.BadgeId, request.Page, request.PageSize, cancellationToken);

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
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Handler for getting a user's badges
/// </summary>
public class GetUserBadgesQueryHandler : IRequestHandler<GetUserBadgesQuery, IEnumerable<UserBadgeDto>>
{
    private readonly IBadgeRepository _badgeRepository;

    public GetUserBadgesQueryHandler(IBadgeRepository badgeRepository)
    {
        _badgeRepository = badgeRepository;
    }

    public async Task<IEnumerable<UserBadgeDto>> Handle(GetUserBadgesQuery request, CancellationToken cancellationToken)
    {
        var userBadges = await _badgeRepository.GetUserBadgesAsync(request.UserId, cancellationToken);

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
