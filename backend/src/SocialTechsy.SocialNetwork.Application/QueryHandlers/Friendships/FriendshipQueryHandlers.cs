using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Friendships;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Friendships;

public class GetFriendsQueryHandler : IRequestHandler<GetFriendsQuery, PaginatedResponse<FriendDto>>
{
    private readonly IFriendshipRepository _friendshipRepository;

    public GetFriendsQueryHandler(IFriendshipRepository friendshipRepository)
    {
        _friendshipRepository = friendshipRepository;
    }

    public async Task<PaginatedResponse<FriendDto>> Handle(GetFriendsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (friendships, totalCount) = await _friendshipRepository.GetFriendsPagedAsync(
            request.UserId, page, pageSize, request.Search, cancellationToken);

        var items = friendships.Select(f =>
        {
            var friend = f.RequesterId == request.UserId ? f.Addressee : f.Requester;
            return new FriendDto
            {
                UserId = friend.UserId,
                Username = friend.Username,
                DisplayName = friend.DisplayName,
                ProfilePicture = friend.ProfilePicture,
                FriendsSince = f.RespondedAt ?? f.CreatedAt
            };
        }).ToList();

        return new PaginatedResponse<FriendDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

public class GetPendingRequestsQueryHandler : IRequestHandler<GetPendingRequestsQuery, IEnumerable<FriendshipDto>>
{
    private readonly IFriendshipRepository _friendshipRepository;

    public GetPendingRequestsQueryHandler(IFriendshipRepository friendshipRepository)
    {
        _friendshipRepository = friendshipRepository;
    }

    public async Task<IEnumerable<FriendshipDto>> Handle(GetPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        var requests = await _friendshipRepository.GetPendingRequestsAsync(request.UserId, cancellationToken);
        return requests.Select(FriendshipMapper.MapToDto);
    }
}

public class GetSentRequestsQueryHandler : IRequestHandler<GetSentRequestsQuery, IEnumerable<FriendshipDto>>
{
    private readonly IFriendshipRepository _friendshipRepository;

    public GetSentRequestsQueryHandler(IFriendshipRepository friendshipRepository)
    {
        _friendshipRepository = friendshipRepository;
    }

    public async Task<IEnumerable<FriendshipDto>> Handle(GetSentRequestsQuery request, CancellationToken cancellationToken)
    {
        var requests = await _friendshipRepository.GetSentRequestsAsync(request.UserId, cancellationToken);
        return requests.Select(FriendshipMapper.MapToDto);
    }
}

public class CheckFriendshipQueryHandler : IRequestHandler<CheckFriendshipQuery, FriendshipStatusDto>
{
    private readonly IFriendshipRepository _friendshipRepository;

    public CheckFriendshipQueryHandler(IFriendshipRepository friendshipRepository)
    {
        _friendshipRepository = friendshipRepository;
    }

    public async Task<FriendshipStatusDto> Handle(CheckFriendshipQuery request, CancellationToken cancellationToken)
    {
        var friendship = await _friendshipRepository.GetFriendshipAsync(request.UserId, request.TargetUserId, cancellationToken);

        return new FriendshipStatusDto
        {
            AreFriends = friendship?.Status == Domain.Entities.FriendshipStatus.Accepted,
            RequestPending = friendship?.Status == Domain.Entities.FriendshipStatus.Pending,
            FriendshipId = friendship?.FriendshipId,
            IsSentByMe = friendship?.RequesterId == request.UserId
        };
    }
}

public class GetFriendSuggestionsQueryHandler : IRequestHandler<GetFriendSuggestionsQuery, IEnumerable<FriendDto>>
{
    private readonly IFriendshipRepository _friendshipRepository;

    public GetFriendSuggestionsQueryHandler(IFriendshipRepository friendshipRepository)
    {
        _friendshipRepository = friendshipRepository;
    }

    public async Task<IEnumerable<FriendDto>> Handle(GetFriendSuggestionsQuery request, CancellationToken cancellationToken)
    {
        return await _friendshipRepository.GetSuggestedFriendsAsync(request.UserId, request.Limit, cancellationToken);
    }
}

public class GetNetworkGrowthQueryHandler : IRequestHandler<GetNetworkGrowthQuery, NetworkGrowthDto>
{
    private readonly IFriendshipRepository _friendshipRepository;

    public GetNetworkGrowthQueryHandler(IFriendshipRepository friendshipRepository)
    {
        _friendshipRepository = friendshipRepository;
    }

    public async Task<NetworkGrowthDto> Handle(GetNetworkGrowthQuery request, CancellationToken cancellationToken)
    {
        return await _friendshipRepository.GetNetworkGrowthAsync(request.UserId, request.Days, cancellationToken);
    }
}

internal static class FriendshipMapper
{
    public static FriendshipDto MapToDto(Domain.Entities.Friendship f) => new()
    {
        FriendshipId = f.FriendshipId,
        Requester = new UserSummaryDto
        {
            UserId = f.Requester.UserId,
            Username = f.Requester.Username,
            DisplayName = f.Requester.DisplayName,
            ProfilePicture = f.Requester.ProfilePicture
        },
        Addressee = new UserSummaryDto
        {
            UserId = f.Addressee.UserId,
            Username = f.Addressee.Username,
            DisplayName = f.Addressee.DisplayName,
            ProfilePicture = f.Addressee.ProfilePicture
        },
        Status = f.Status.ToString(),
        CreatedAt = f.CreatedAt,
        RespondedAt = f.RespondedAt
    };
}
