using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Application.Commands.Friendships;

/// <summary>
/// Command for sending a friend request
/// </summary>
public class SendFriendRequestCommand : IRequest<FriendshipDto>
{
    public int RequesterId { get; set; }
    public int AddresseeId { get; set; }
}

/// <summary>
/// Command for accepting a friend request
/// </summary>
public class AcceptFriendRequestCommand : IRequest<FriendshipDto>
{
    public int FriendshipId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command for rejecting a friend request
/// </summary>
public class RejectFriendRequestCommand : IRequest<bool>
{
    public int FriendshipId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command for deleting/canceling a friendship or request
/// </summary>
public class DeleteFriendshipCommand : IRequest<bool>
{
    public int FriendshipId { get; set; }
    public int UserId { get; set; }
}
