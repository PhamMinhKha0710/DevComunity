using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using System.Security.Claims;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Groups functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly ILogger<GroupsController> _logger;
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;

    public GroupsController(
        ILogger<GroupsController> logger,
        IGroupRepository groupRepository,
        IUserRepository userRepository)
    {
        _logger = logger;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get paginated list of groups
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<GroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<GroupDto>>> GetGroups(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var (items, totalCount) = await _groupRepository.GetPaginatedAsync(page, pageSize, search, cancellationToken);

        var groups = new List<GroupDto>();
        foreach (var g in items)
        {
            var memberCount = await _groupRepository.GetMemberCountAsync(g.GroupId, cancellationToken);
            var isMember = currentUserId > 0 && await _groupRepository.IsMemberAsync(g.GroupId, currentUserId, cancellationToken);
            var currentMember = currentUserId > 0 ? await _groupRepository.GetMemberAsync(g.GroupId, currentUserId, cancellationToken) : null;

            groups.Add(new GroupDto
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
                MemberCount = memberCount,
                IsMember = isMember,
                CurrentUserRole = currentMember?.Role.ToString()
            });
        }

        return Ok(new PaginatedResponse<GroupDto>
        {
            Items = groups,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get current user's groups
    /// </summary>
    [HttpGet("my-groups")]
    [ProducesResponseType(typeof(IEnumerable<GroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GroupDto>>> GetMyGroups(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var groups = await _groupRepository.GetUserGroupsAsync(userId, cancellationToken);
        
        var result = new List<GroupDto>();
        foreach (var g in groups)
        {
            var memberCount = await _groupRepository.GetMemberCountAsync(g.GroupId, cancellationToken);
            var currentMember = await _groupRepository.GetMemberAsync(g.GroupId, userId, cancellationToken);

            result.Add(new GroupDto
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
                MemberCount = memberCount,
                IsMember = true,
                CurrentUserRole = currentMember?.Role.ToString()
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get group details by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDto>> GetGroup(int id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var group = await _groupRepository.GetByIdAsync(id, cancellationToken);
        
        if (group == null) return NotFound(new { message = "Group not found" });

        // Private groups require membership to view
        if (group.IsPrivate && currentUserId > 0)
        {
            var isMember = await _groupRepository.IsMemberAsync(id, currentUserId, cancellationToken);
            if (!isMember) return Forbid();
        }

        var memberCount = await _groupRepository.GetMemberCountAsync(id, cancellationToken);
        var isMemberCheck = currentUserId > 0 && await _groupRepository.IsMemberAsync(id, currentUserId, cancellationToken);
        var currentMember = currentUserId > 0 ? await _groupRepository.GetMemberAsync(id, currentUserId, cancellationToken) : null;

        return Ok(new GroupDto
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
        });
    }

    /// <summary>
    /// Get group members
    /// </summary>
    [HttpGet("{id:int}/members")]
    [ProducesResponseType(typeof(IEnumerable<GroupMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GroupMemberDto>>> GetMembers(int id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0) return Unauthorized();

        var group = await _groupRepository.GetByIdAsync(id, cancellationToken);
        if (group == null) return NotFound(new { message = "Group not found" });

        // Check if user can view members
        if (group.IsPrivate)
        {
            var isMember = await _groupRepository.IsMemberAsync(id, currentUserId, cancellationToken);
            if (!isMember) return Forbid();
        }

        var members = await _groupRepository.GetMembersAsync(id, cancellationToken);
        
        return Ok(members.Select(m => new GroupMemberDto
        {
            UserId = m.User.UserId,
            Username = m.User.Username,
            DisplayName = m.User.DisplayName,
            ProfilePicture = m.User.ProfilePicture,
            Role = m.Role.ToString(),
            JoinedAt = m.JoinedAt
        }));
    }

    /// <summary>
    /// Create a new group
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<GroupDto>> CreateGroup(
        [FromBody] CreateGroupRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var group = new Group
        {
            Name = request.Name,
            Description = request.Description,
            CreatorId = userId,
            IsPrivate = request.IsPrivate
        };

        var created = await _groupRepository.AddAsync(group, cancellationToken);

        // Add creator as admin
        var member = new GroupMember
        {
            GroupId = created.GroupId,
            UserId = userId,
            Role = GroupRole.Admin
        };
        await _groupRepository.AddMemberAsync(member, cancellationToken);

        // Reload with navigation properties
        var result = await _groupRepository.GetByIdAsync(created.GroupId, cancellationToken);

        _logger.LogInformation("User {UserId} created group {GroupId}: {GroupName}", userId, created.GroupId, created.Name);

        return Created($"/api/groups/{result!.GroupId}", new GroupDto
        {
            GroupId = result.GroupId,
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
        });
    }

    /// <summary>
    /// Update group details
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GroupDto>> UpdateGroup(
        int id, 
        [FromBody] UpdateGroupRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var group = await _groupRepository.GetByIdAsync(id, cancellationToken);
        if (group == null) return NotFound(new { message = "Group not found" });

        // Check if user is admin
        var member = await _groupRepository.GetMemberAsync(id, userId, cancellationToken);
        if (member == null || member.Role != GroupRole.Admin)
            return Forbid();

        if (request.Name != null) group.Name = request.Name;
        if (request.Description != null) group.Description = request.Description;
        if (request.IsPrivate.HasValue) group.IsPrivate = request.IsPrivate.Value;

        await _groupRepository.UpdateAsync(group, cancellationToken);

        _logger.LogInformation("User {UserId} updated group {GroupId}", userId, id);

        // Reload and return updated group
        var updatedGroup = await _groupRepository.GetByIdAsync(id, cancellationToken);
        var memberCount = await _groupRepository.GetMemberCountAsync(id, cancellationToken);

        return Ok(new GroupDto
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
        });

    }

    /// <summary>
    /// Delete a group
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteGroup(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var group = await _groupRepository.GetByIdAsync(id, cancellationToken);
        if (group == null) return NotFound(new { message = "Group not found" });

        // Only creator can delete
        if (group.CreatorId != userId) return Forbid();

        await _groupRepository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation("User {UserId} deleted group {GroupId}", userId, id);

        return Ok(new { message = "Group deleted" });
    }

    /// <summary>
    /// Join a group
    /// </summary>
    [HttpPost("{id:int}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> JoinGroup(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var group = await _groupRepository.GetByIdAsync(id, cancellationToken);
        if (group == null) return NotFound(new { message = "Group not found" });

        // Check if already a member
        var isMember = await _groupRepository.IsMemberAsync(id, userId, cancellationToken);
        if (isMember) return BadRequest(new { message = "Already a member of this group" });

        // For private groups, need to be invited (not implemented yet)
        if (group.IsPrivate)
            return BadRequest(new { message = "This is a private group. You need an invitation to join." });

        var member = new GroupMember
        {
            GroupId = id,
            UserId = userId,
            Role = GroupRole.Member
        };
        await _groupRepository.AddMemberAsync(member, cancellationToken);

        _logger.LogInformation("User {UserId} joined group {GroupId}", userId, id);

        return Ok(new { message = "Successfully joined group" });
    }

    /// <summary>
    /// Leave a group
    /// </summary>
    [HttpPost("{id:int}/leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LeaveGroup(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var group = await _groupRepository.GetByIdAsync(id, cancellationToken);
        if (group == null) return NotFound(new { message = "Group not found" });

        // Creator cannot leave
        if (group.CreatorId == userId)
            return BadRequest(new { message = "Group creator cannot leave. Transfer ownership or delete the group." });

        await _groupRepository.RemoveMemberAsync(id, userId, cancellationToken);

        _logger.LogInformation("User {UserId} left group {GroupId}", userId, id);

        return Ok(new { message = "Successfully left group" });
    }

    /// <summary>
    /// Update member role (Admin only)
    /// </summary>
    [HttpPut("{id:int}/members/{memberId:int}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMemberRole(
        int id, 
        int memberId, 
        [FromBody] UpdateMemberRoleRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var group = await _groupRepository.GetByIdAsync(id, cancellationToken);
        if (group == null) return NotFound(new { message = "Group not found" });

        // Check if current user is admin
        var currentMember = await _groupRepository.GetMemberAsync(id, userId, cancellationToken);
        if (currentMember == null || currentMember.Role != GroupRole.Admin)
            return Forbid();

        // Check if target member exists
        var targetMember = await _groupRepository.GetMemberAsync(id, memberId, cancellationToken);
        if (targetMember == null) return NotFound(new { message = "Member not found" });

        // Cannot change creator's role
        if (group.CreatorId == memberId)
            return BadRequest(new { message = "Cannot change group creator's role" });

        if (!Enum.TryParse<GroupRole>(request.Role, true, out var newRole))
            return BadRequest(new { message = "Invalid role. Use 'Member', 'Moderator', or 'Admin'" });

        await _groupRepository.UpdateMemberRoleAsync(id, memberId, newRole, cancellationToken);

        _logger.LogInformation("User {UserId} updated member {MemberId} role to {Role} in group {GroupId}", 
            userId, memberId, newRole, id);

        return Ok(new { message = $"Member role updated to {newRole}" });
    }

    /// <summary>
    /// Remove a member from group (Admin/Moderator only)
    /// </summary>
    [HttpDelete("{id:int}/members/{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveMember(int id, int memberId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var group = await _groupRepository.GetByIdAsync(id, cancellationToken);
        if (group == null) return NotFound(new { message = "Group not found" });

        // Check if current user has permission
        var currentMember = await _groupRepository.GetMemberAsync(id, userId, cancellationToken);
        if (currentMember == null || (currentMember.Role != GroupRole.Admin && currentMember.Role != GroupRole.Moderator))
            return Forbid();

        // Cannot remove creator
        if (group.CreatorId == memberId)
            return BadRequest(new { message = "Cannot remove group creator" });

        // Moderators cannot remove admins
        var targetMember = await _groupRepository.GetMemberAsync(id, memberId, cancellationToken);
        if (targetMember == null) return NotFound(new { message = "Member not found" });
        if (currentMember.Role == GroupRole.Moderator && targetMember.Role == GroupRole.Admin)
            return Forbid();

        await _groupRepository.RemoveMemberAsync(id, memberId, cancellationToken);

        _logger.LogInformation("User {UserId} removed member {MemberId} from group {GroupId}", userId, memberId, id);

        return Ok(new { message = "Member removed from group" });
    }
}
