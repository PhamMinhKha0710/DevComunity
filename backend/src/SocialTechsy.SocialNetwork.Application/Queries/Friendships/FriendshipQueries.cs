using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

namespace SocialTechsy.SocialNetwork.Application.Queries.Friendships;

/// <summary>
/// Query for getting current user's friends list (paginated)
/// </summary>
public class GetFriendsQuery : IRequest<PaginatedResponse<FriendDto>>
{
    public int UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}

/// <summary>
/// Query for getting pending friend requests (received)
/// </summary>
public class GetPendingRequestsQuery : IRequest<IEnumerable<FriendshipDto>>
{
    public int UserId { get; set; }
}

/// <summary>
/// Query for getting sent friend requests
/// </summary>
public class GetSentRequestsQuery : IRequest<IEnumerable<FriendshipDto>>
{
    public int UserId { get; set; }
}

/// <summary>
/// Query for checking friendship status between two users
/// </summary>
public class CheckFriendshipQuery : IRequest<FriendshipStatusDto>
{
    public int UserId { get; set; }
    public int TargetUserId { get; set; }
}

/// <summary>
/// Query for getting suggested friends
/// </summary>
public class GetFriendSuggestionsQuery : IRequest<IEnumerable<FriendDto>>
{
    public int UserId { get; set; }
    public int Limit { get; set; } = 5;
}

/// <summary>
/// Query for getting network growth analytics
/// </summary>
public class GetNetworkGrowthQuery : IRequest<NetworkGrowthDto>
{
    public int UserId { get; set; }
    public int Days { get; set; } = 28;
}
