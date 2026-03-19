using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Follows;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Follows;

public class GetFollowersQueryHandler : IRequestHandler<GetFollowersQuery, IEnumerable<FollowDto>>
{
    private readonly IFollowRepository _followRepository;

    public GetFollowersQueryHandler(IFollowRepository followRepository)
    {
        _followRepository = followRepository;
    }

    public async Task<IEnumerable<FollowDto>> Handle(GetFollowersQuery request, CancellationToken cancellationToken)
    {
        var followers = await _followRepository.GetFollowersAsync(request.UserId, cancellationToken);

        return followers.Select(f => new FollowDto
        {
            UserId = f.Follower.UserId,
            Username = f.Follower.Username,
            DisplayName = f.Follower.DisplayName,
            ProfilePicture = f.Follower.ProfilePicture,
            FollowedAt = f.CreatedAt
        });
    }
}

public class GetFollowingQueryHandler : IRequestHandler<GetFollowingQuery, IEnumerable<FollowDto>>
{
    private readonly IFollowRepository _followRepository;

    public GetFollowingQueryHandler(IFollowRepository followRepository)
    {
        _followRepository = followRepository;
    }

    public async Task<IEnumerable<FollowDto>> Handle(GetFollowingQuery request, CancellationToken cancellationToken)
    {
        var following = await _followRepository.GetFollowingAsync(request.UserId, cancellationToken);

        return following.Select(f => new FollowDto
        {
            UserId = f.Following.UserId,
            Username = f.Following.Username,
            DisplayName = f.Following.DisplayName,
            ProfilePicture = f.Following.ProfilePicture,
            FollowedAt = f.CreatedAt
        });
    }
}

public class GetFollowStatsQueryHandler : IRequestHandler<GetFollowStatsQuery, FollowStatsDto>
{
    private readonly IFollowRepository _followRepository;

    public GetFollowStatsQueryHandler(IFollowRepository followRepository)
    {
        _followRepository = followRepository;
    }

    public async Task<FollowStatsDto> Handle(GetFollowStatsQuery request, CancellationToken cancellationToken)
    {
        var followersCount = await _followRepository.GetFollowersCountAsync(request.UserId, cancellationToken);
        var followingCount = await _followRepository.GetFollowingCountAsync(request.UserId, cancellationToken);
        var isFollowing = request.CurrentUserId > 0
            && await _followRepository.IsFollowingAsync(request.CurrentUserId.Value, request.UserId, cancellationToken);

        return new FollowStatsDto
        {
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            IsFollowing = isFollowing
        };
    }
}

public class CheckFollowingQueryHandler : IRequestHandler<CheckFollowingQuery, bool>
{
    private readonly IFollowRepository _followRepository;

    public CheckFollowingQueryHandler(IFollowRepository followRepository)
    {
        _followRepository = followRepository;
    }

    public async Task<bool> Handle(CheckFollowingQuery request, CancellationToken cancellationToken)
    {
        return await _followRepository.IsFollowingAsync(request.FollowerId, request.FollowingId, cancellationToken);
    }
}
