using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Groups;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Groups;

public class GetGroupsQueryHandler : IRequestHandler<GetGroupsQuery, PaginatedResponse<GroupDto>>
{
    private readonly IGroupRepository _groupRepository;

    public GetGroupsQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<PaginatedResponse<GroupDto>> Handle(GetGroupsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _groupRepository.GetPaginatedAsync(
            request.Page, request.PageSize, request.Search, cancellationToken);

        var groupIds = items.Select(g => g.GroupId).ToList();
        var memberCounts = await _groupRepository.GetMemberCountsBatchAsync(groupIds, cancellationToken);
        var memberships = request.CurrentUserId > 0
            ? await _groupRepository.GetUserMembershipsBatchAsync(groupIds, request.CurrentUserId.Value, cancellationToken)
            : new Dictionary<int, Domain.Entities.GroupMember?>();

        var groups = items.Select(g => new GroupDto
        {
            GroupId = g.GroupId,
            Name = g.Name,
            Description = g.Description,
            CoverImage = g.CoverImage,
            Creator = new UserSummaryDto
            {
                UserId = g.Creator.UserId,
                Username = g.Creator.Username,
                DisplayName = g.Creator.DisplayName,
                ProfilePicture = g.Creator.ProfilePicture
            },
            IsPrivate = g.IsPrivate,
            CreatedAt = g.CreatedAt,
            MemberCount = memberCounts.GetValueOrDefault(g.GroupId, 0),
            IsMember = memberships.ContainsKey(g.GroupId) && memberships[g.GroupId] != null,
            CurrentUserRole = memberships.GetValueOrDefault(g.GroupId)?.Role.ToString()
        }).ToList();

        return new PaginatedResponse<GroupDto>
        {
            Items = groups,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

public class GetMyGroupsQueryHandler : IRequestHandler<GetMyGroupsQuery, IEnumerable<GroupDto>>
{
    private readonly IGroupRepository _groupRepository;

    public GetMyGroupsQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<IEnumerable<GroupDto>> Handle(GetMyGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = await _groupRepository.GetUserGroupsAsync(request.UserId, cancellationToken);
        var groupList = groups.ToList();

        var groupIds = groupList.Select(g => g.GroupId).ToList();
        var memberCounts = await _groupRepository.GetMemberCountsBatchAsync(groupIds, cancellationToken);
        var memberships = await _groupRepository.GetUserMembershipsBatchAsync(groupIds, request.UserId, cancellationToken);

        return groupList.Select(g => new GroupDto
        {
            GroupId = g.GroupId,
            Name = g.Name,
            Description = g.Description,
            CoverImage = g.CoverImage,
            Creator = new UserSummaryDto
            {
                UserId = g.Creator.UserId,
                Username = g.Creator.Username,
                DisplayName = g.Creator.DisplayName,
                ProfilePicture = g.Creator.ProfilePicture
            },
            IsPrivate = g.IsPrivate,
            CreatedAt = g.CreatedAt,
            MemberCount = memberCounts.GetValueOrDefault(g.GroupId, 0),
            IsMember = true,
            CurrentUserRole = memberships.GetValueOrDefault(g.GroupId)?.Role.ToString()
        }).ToList();
    }
}

public class GetGroupByIdQueryHandler : IRequestHandler<GetGroupByIdQuery, GroupDto?>
{
    private readonly IGroupRepository _groupRepository;

    public GetGroupByIdQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<GroupDto?> Handle(GetGroupByIdQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group == null) return null;

        if (group.IsPrivate && request.CurrentUserId > 0)
        {
            var isMember = await _groupRepository.IsMemberAsync(request.GroupId, request.CurrentUserId.Value, cancellationToken);
            if (!isMember)
                throw new UnauthorizedAccessException("Not a member of this private group");
        }

        var memberCount = await _groupRepository.GetMemberCountAsync(request.GroupId, cancellationToken);
        var isMemberCheck = request.CurrentUserId > 0
            && await _groupRepository.IsMemberAsync(request.GroupId, request.CurrentUserId.Value, cancellationToken);
        var currentMember = request.CurrentUserId > 0
            ? await _groupRepository.GetMemberAsync(request.GroupId, request.CurrentUserId.Value, cancellationToken)
            : null;

        return new GroupDto
        {
            GroupId = group.GroupId,
            Name = group.Name,
            Description = group.Description,
            CoverImage = group.CoverImage,
            Creator = new UserSummaryDto
            {
                UserId = group.Creator.UserId,
                Username = group.Creator.Username,
                DisplayName = group.Creator.DisplayName,
                ProfilePicture = group.Creator.ProfilePicture
            },
            IsPrivate = group.IsPrivate,
            CreatedAt = group.CreatedAt,
            MemberCount = memberCount,
            IsMember = isMemberCheck,
            CurrentUserRole = currentMember?.Role.ToString()
        };
    }
}

public class IsGroupMemberQueryHandler : IRequestHandler<IsGroupMemberQuery, bool>
{
    private readonly IGroupRepository _groupRepository;

    public IsGroupMemberQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<bool> Handle(IsGroupMemberQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new InvalidOperationException("Group not found");

        return await _groupRepository.IsMemberAsync(request.GroupId, request.UserId, cancellationToken);
    }
}

public class GetGroupMembersQueryHandler : IRequestHandler<GetGroupMembersQuery, IEnumerable<GroupMemberDto>>
{
    private readonly IGroupRepository _groupRepository;

    public GetGroupMembersQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<IEnumerable<GroupMemberDto>> Handle(GetGroupMembersQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new InvalidOperationException("Group not found");

        if (group.IsPrivate)
        {
            var isMember = await _groupRepository.IsMemberAsync(request.GroupId, request.CurrentUserId, cancellationToken);
            if (!isMember)
                throw new UnauthorizedAccessException("Not a member of this private group");
        }

        var members = await _groupRepository.GetMembersAsync(request.GroupId, cancellationToken);

        return members.Select(m => new GroupMemberDto
        {
            UserId = m.User.UserId,
            Username = m.User.Username,
            DisplayName = m.User.DisplayName,
            ProfilePicture = m.User.ProfilePicture,
            Role = m.Role.ToString(),
            JoinedAt = m.JoinedAt
        });
    }
}
