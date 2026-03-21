using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

namespace SocialTechsy.SocialNetwork.Application.Queries.Follows;

/// <summary>
/// Query for getting followers of a user
/// </summary>
public class GetFollowersQuery : IRequest<IEnumerable<FollowDto>>
{
    public int UserId { get; set; }
}

/// <summary>
/// Query for getting users that a user is following
/// </summary>
public class GetFollowingQuery : IRequest<IEnumerable<FollowDto>>
{
    public int UserId { get; set; }
}

/// <summary>
/// Query for getting follow statistics
/// </summary>
public class GetFollowStatsQuery : IRequest<FollowStatsDto>
{
    public int UserId { get; set; }
    public int? CurrentUserId { get; set; }
}

/// <summary>
/// Query for checking if current user is following a target user
/// </summary>
public class CheckFollowingQuery : IRequest<bool>
{
    public int FollowerId { get; set; }
    public int FollowingId { get; set; }
}
