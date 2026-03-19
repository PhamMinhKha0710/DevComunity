using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Queries.Groups;
using SocialTechsy.SocialNetwork.Application.Commands.Groups;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly ILogger<GroupsController> _logger;
    private readonly IMediator _mediator;

    public GroupsController(ILogger<GroupsController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<GroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<GroupDto>>> GetGroups(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetGroupsQuery
            {
                CurrentUserId = GetCurrentUserId() > 0 ? GetCurrentUserId() : null,
                Page = page,
                PageSize = pageSize,
                Search = search
            }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-groups")]
    [HttpGet("my")]
    [ProducesResponseType(typeof(IEnumerable<GroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GroupDto>>> GetMyGroups(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _mediator.Send(new GetMyGroupsQuery { UserId = userId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDto>> GetGroup(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new GetGroupByIdQuery
                {
                    GroupId = id,
                    CurrentUserId = GetCurrentUserId() > 0 ? GetCurrentUserId() : null
                }, cancellationToken);

            if (result == null) return NotFound(new { message = "Group not found" });
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id:int}/isMember")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> IsMember(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new IsGroupMemberQuery { GroupId = id, UserId = GetCurrentUserId() }, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Group not found" });
        }
    }

    [HttpGet("{id:int}/members")]
    [ProducesResponseType(typeof(IEnumerable<GroupMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GroupMemberDto>>> GetMembers(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _mediator.Send(
                new GetGroupMembersQuery { GroupId = id, CurrentUserId = userId },
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Group not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<GroupDto>> CreateGroup(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _mediator.Send(
            new CreateGroupCommand
            {
                CreatorId = userId,
                Name = request.Name,
                Description = request.Description,
                IsPrivate = request.IsPrivate
            }, cancellationToken);

        return Created($"/api/groups/{result.GroupId}", result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GroupDto>> UpdateGroup(
        int id,
        [FromBody] UpdateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _mediator.Send(
                new UpdateGroupCommand
                {
                    GroupId = id,
                    UserId = userId,
                    Name = request.Name,
                    Description = request.Description,
                    IsPrivate = request.IsPrivate
                }, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Group not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteGroup(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            await _mediator.Send(new DeleteGroupCommand { GroupId = id, UserId = userId }, cancellationToken);
            return Ok(new { message = "Group deleted" });
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Group not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:int}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> JoinGroup(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            await _mediator.Send(new JoinGroupCommand { GroupId = id, UserId = userId }, cancellationToken);
            return Ok(new { message = "Successfully joined group" });
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("private")
                ? BadRequest(new { message = ex.Message })
                : NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LeaveGroup(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            await _mediator.Send(new LeaveGroupCommand { GroupId = id, UserId = userId }, cancellationToken);
            return Ok(new { message = "Successfully left group" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

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

        try
        {
            await _mediator.Send(
                new UpdateMemberRoleCommand
                {
                    GroupId = id,
                    CurrentUserId = userId,
                    TargetUserId = memberId,
                    Role = request.Role
                }, cancellationToken);
            return Ok(new { message = $"Member role updated to {request.Role}" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:int}/members/{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveMember(int id, int memberId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            await _mediator.Send(
                new RemoveMemberCommand
                {
                    GroupId = id,
                    CurrentUserId = userId,
                    TargetUserId = memberId
                }, cancellationToken);
            return Ok(new { message = "Member removed from group" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
