using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Queries.Follows;
using SocialTechsy.SocialNetwork.Application.Commands.Follows;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FollowController : ControllerBase
{
    private readonly ILogger<FollowController> _logger;
    private readonly IMediator _mediator;

    public FollowController(ILogger<FollowController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet("followers/{userId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<FollowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FollowDto>>> GetFollowers(int userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFollowersQuery { UserId = userId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("following/{userId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<FollowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FollowDto>>> GetFollowing(int userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFollowingQuery { UserId = userId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("stats/{userId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FollowStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FollowStatsDto>> GetFollowStats(int userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetFollowStatsQuery { UserId = userId, CurrentUserId = GetCurrentUserId() > 0 ? GetCurrentUserId() : null },
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("{targetUserId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Follow(int targetUserId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();
        if (userId == targetUserId) return BadRequest(new { message = "Cannot follow yourself" });

        try
        {
            await _mediator.Send(new FollowCommand { FollowerId = userId, FollowingId = targetUserId }, cancellationToken);
            return Ok(new { message = "Successfully followed user" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{targetUserId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Unfollow(int targetUserId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        await _mediator.Send(new UnfollowCommand { FollowerId = userId, FollowingId = targetUserId }, cancellationToken);
        return Ok(new { message = "Successfully unfollowed user" });
    }

    [HttpGet("check/{targetUserId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> CheckFollowing(int targetUserId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _mediator.Send(
            new CheckFollowingQuery { FollowerId = userId, FollowingId = targetUserId },
            cancellationToken);
        return Ok(new { isFollowing = result });
    }
}
