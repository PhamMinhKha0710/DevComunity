using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Application.Queries.Friendships;

/// <summary>
/// Query for getting current user's friends list
/// </summary>
public class GetFriendsQuery : IRequest<IEnumerable<FriendDto>>
{
    public int UserId { get; set; }
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
