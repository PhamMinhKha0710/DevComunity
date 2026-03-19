using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Application.Commands.Groups;

/// <summary>
/// Command for creating a new group
/// </summary>
public class CreateGroupCommand : IRequest<GroupDto>
{
    public int CreatorId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
}

/// <summary>
/// Command for updating a group
/// </summary>
public class UpdateGroupCommand : IRequest<GroupDto>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsPrivate { get; set; }
}

/// <summary>
/// Command for deleting a group
/// </summary>
public class DeleteGroupCommand : IRequest<bool>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command for joining a group
/// </summary>
public class JoinGroupCommand : IRequest<bool>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command for leaving a group
/// </summary>
public class LeaveGroupCommand : IRequest<bool>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command for updating member role
/// </summary>
public class UpdateMemberRoleCommand : IRequest<bool>
{
    public int GroupId { get; set; }
    public int CurrentUserId { get; set; }
    public int TargetUserId { get; set; }
    public string Role { get; set; } = null!;
}

/// <summary>
/// Command for removing a member from a group
/// </summary>
public class RemoveMemberCommand : IRequest<bool>
{
    public int GroupId { get; set; }
    public int CurrentUserId { get; set; }
    public int TargetUserId { get; set; }
}
