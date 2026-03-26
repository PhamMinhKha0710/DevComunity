using MediatR;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Commands.Groups;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Groups;

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, GroupDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateGroupCommandHandler> _logger;

    public CreateGroupCommandHandler(
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateGroupCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupDto> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var group = Group.Create(
            request.CreatorId,
            request.Name,
            request.Description,
            request.IsPrivate);

        var created = await _groupRepository.AddAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var member = new GroupMember
        {
            GroupId = created.GroupId,
            UserId = request.CreatorId,
            Role = GroupRole.Admin
        };
        await _groupRepository.AddMemberAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = await _groupRepository.GetByIdAsync(created.GroupId, cancellationToken);

        _logger.LogInformation("User {UserId} created group {GroupId}: {GroupName}", request.CreatorId, created.GroupId, created.Name);

        return new GroupDto
        {
            GroupId = result!.GroupId,
            Name = result.Name,
            Description = result.Description,
            CoverImage = result.CoverImage,
            Creator = new UserSummaryDto
            {
                UserId = result.Creator.UserId,
                Username = result.Creator.Username,
                DisplayName = result.Creator.DisplayName,
                ProfilePicture = result.Creator.ProfilePicture
            },
            IsPrivate = result.IsPrivate,
            CreatedAt = result.CreatedAt,
            MemberCount = 1,
            IsMember = true,
            CurrentUserRole = "Admin"
        };
    }
}

public class UpdateGroupCommandHandler : IRequestHandler<UpdateGroupCommand, GroupDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateGroupCommandHandler> _logger;

    public UpdateGroupCommandHandler(
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateGroupCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupDto> Handle(UpdateGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new InvalidOperationException("Group not found");

        var member = await _groupRepository.GetMemberAsync(request.GroupId, request.UserId, cancellationToken);
        if (member == null || member.Role != GroupRole.Admin)
            throw new UnauthorizedAccessException("Only admins can update the group");

        group.UpdateInfo(request.Name!, request.Description, request.IsPrivate);

        await _groupRepository.UpdateAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} updated group {GroupId}", request.UserId, request.GroupId);

        var updatedGroup = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        var memberCount = await _groupRepository.GetMemberCountAsync(request.GroupId, cancellationToken);

        return new GroupDto
        {
            GroupId = updatedGroup!.GroupId,
            Name = updatedGroup.Name,
            Description = updatedGroup.Description,
            CoverImage = updatedGroup.CoverImage,
            Creator = new UserSummaryDto
            {
                UserId = updatedGroup.Creator.UserId,
                Username = updatedGroup.Creator.Username,
                DisplayName = updatedGroup.Creator.DisplayName,
                ProfilePicture = updatedGroup.Creator.ProfilePicture
            },
            IsPrivate = updatedGroup.IsPrivate,
            CreatedAt = updatedGroup.CreatedAt,
            MemberCount = memberCount,
            IsMember = true,
            CurrentUserRole = member.Role.ToString()
        };
    }
}

public class DeleteGroupCommandHandler : IRequestHandler<DeleteGroupCommand, bool>
{
    private readonly IGroupRepository _groupRepository;
    private readonly ILogger<DeleteGroupCommandHandler> _logger;

    public DeleteGroupCommandHandler(
        IGroupRepository groupRepository,
        ILogger<DeleteGroupCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new InvalidOperationException("Group not found");

        if (group.CreatorId != request.UserId)
            throw new UnauthorizedAccessException("Only the creator can delete the group");

        await _groupRepository.DeleteAsync(request.GroupId, cancellationToken);

        _logger.LogInformation("User {UserId} deleted group {GroupId}", request.UserId, request.GroupId);

        return true;
    }
}

public class JoinGroupCommandHandler : IRequestHandler<JoinGroupCommand, bool>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<JoinGroupCommandHandler> _logger;

    public JoinGroupCommandHandler(
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        INotificationDispatcher notificationDispatcher,
        ILogger<JoinGroupCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task<bool> Handle(JoinGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new InvalidOperationException("Group not found");

        var isMember = await _groupRepository.IsMemberAsync(request.GroupId, request.UserId, cancellationToken);
        if (isMember)
            return true;

        if (group.IsPrivate)
            throw new InvalidOperationException("This is a private group. You need an invitation to join.");

        var member = new GroupMember
        {
            GroupId = request.GroupId,
            UserId = request.UserId,
            Role = GroupRole.Member
        };
        await _groupRepository.AddMemberAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} joined group {GroupId}", request.UserId, request.GroupId);

        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            var notification = new Notification
            {
                UserId = group.CreatorId,
                Type = "GroupJoin",
                Message = $"{user?.DisplayName ?? user?.Username} joined your group {group.Name}",
                Link = $"/groups/{group.GroupId}",
                CreatedDate = DateTime.UtcNow
            };
            await _notificationDispatcher.DispatchAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending join notification");
        }

        return true;
    }
}

public class LeaveGroupCommandHandler : IRequestHandler<LeaveGroupCommand, bool>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<LeaveGroupCommandHandler> _logger;

    public LeaveGroupCommandHandler(
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        INotificationDispatcher notificationDispatcher,
        ILogger<LeaveGroupCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task<bool> Handle(LeaveGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new InvalidOperationException("Group not found");

        if (group.CreatorId == request.UserId)
            throw new InvalidOperationException("Group creator cannot leave. Transfer ownership or delete the group.");

        var isMember = await _groupRepository.IsMemberAsync(request.GroupId, request.UserId, cancellationToken);
        if (!isMember)
            return true;

        await _groupRepository.RemoveMemberAsync(request.GroupId, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} left group {GroupId}", request.UserId, request.GroupId);

        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            var notification = new Notification
            {
                UserId = group.CreatorId,
                Type = "GroupLeave",
                Message = $"{user?.DisplayName ?? user?.Username} left your group {group.Name}",
                Link = $"/groups/{group.GroupId}",
                CreatedDate = DateTime.UtcNow
            };
            await _notificationDispatcher.DispatchAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending leave notification");
        }

        return true;
    }
}

public class UpdateMemberRoleCommandHandler : IRequestHandler<UpdateMemberRoleCommand, bool>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateMemberRoleCommandHandler> _logger;

    public UpdateMemberRoleCommandHandler(
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateMemberRoleCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new InvalidOperationException("Group not found");

        var currentMember = await _groupRepository.GetMemberAsync(request.GroupId, request.CurrentUserId, cancellationToken);
        if (currentMember == null || currentMember.Role != GroupRole.Admin)
            throw new UnauthorizedAccessException("Only admins can update member roles");

        var targetMember = await _groupRepository.GetMemberAsync(request.GroupId, request.TargetUserId, cancellationToken);
        if (targetMember == null)
            throw new InvalidOperationException("Member not found");

        if (group.CreatorId == request.TargetUserId)
            throw new InvalidOperationException("Cannot change group creator's role");

        if (!Enum.TryParse<GroupRole>(request.Role, true, out var newRole))
            throw new InvalidOperationException("Invalid role. Use 'Member', 'Moderator', or 'Admin'");

        await _groupRepository.UpdateMemberRoleAsync(request.GroupId, request.TargetUserId, newRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} updated member {TargetUserId} role to {Role} in group {GroupId}",
            request.CurrentUserId, request.TargetUserId, newRole, request.GroupId);

        return true;
    }
}

public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, bool>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveMemberCommandHandler> _logger;

    public RemoveMemberCommandHandler(
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveMemberCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new InvalidOperationException("Group not found");

        var currentMember = await _groupRepository.GetMemberAsync(request.GroupId, request.CurrentUserId, cancellationToken);
        if (currentMember == null || (currentMember.Role != GroupRole.Admin && currentMember.Role != GroupRole.Moderator))
            throw new UnauthorizedAccessException("Not authorized to remove members");

        if (group.CreatorId == request.TargetUserId)
            throw new InvalidOperationException("Cannot remove group creator");

        var targetMember = await _groupRepository.GetMemberAsync(request.GroupId, request.TargetUserId, cancellationToken);
        if (targetMember == null)
            throw new InvalidOperationException("Member not found");

        if (currentMember.Role == GroupRole.Moderator && targetMember.Role == GroupRole.Admin)
            throw new UnauthorizedAccessException("Moderators cannot remove admins");

        await _groupRepository.RemoveMemberAsync(request.GroupId, request.TargetUserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} removed member {TargetUserId} from group {GroupId}",
            request.CurrentUserId, request.TargetUserId, request.GroupId);

        return true;
    }
}
