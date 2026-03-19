using MediatR;

namespace SocialTechsy.SocialNetwork.Application.Commands.Follows;

/// <summary>
/// Command for following a user
/// </summary>
public class FollowCommand : IRequest<bool>
{
    public int FollowerId { get; set; }
    public int FollowingId { get; set; }
}

/// <summary>
/// Command for unfollowing a user
/// </summary>
public class UnfollowCommand : IRequest<bool>
{
    public int FollowerId { get; set; }
    public int FollowingId { get; set; }
}
