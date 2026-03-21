using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;

namespace SocialTechsy.SocialNetwork.Application.Queries.Groups;

/// <summary>
/// Query for getting paginated list of groups
/// </summary>
public class GetGroupsQuery : IRequest<PaginatedResponse<GroupDto>>
{
    public int? CurrentUserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}

/// <summary>
/// Query for getting current user's groups
/// </summary>
public class GetMyGroupsQuery : IRequest<IEnumerable<GroupDto>>
{
    public int UserId { get; set; }
}

/// <summary>
/// Query for getting a single group by ID
/// </summary>
public class GetGroupByIdQuery : IRequest<GroupDto?>
{
    public int GroupId { get; set; }
    public int? CurrentUserId { get; set; }
}

/// <summary>
/// Query for checking if user is a member of a group
/// </summary>
public class IsGroupMemberQuery : IRequest<bool>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Query for getting group members
/// </summary>
public class GetGroupMembersQuery : IRequest<IEnumerable<GroupMemberDto>>
{
    public int GroupId { get; set; }
    public int CurrentUserId { get; set; }
}
