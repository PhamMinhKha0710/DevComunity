using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Queries.Friendships;
using SocialTechsy.SocialNetwork.Application.Commands.Friendships;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendshipController : ControllerBase
{
    private readonly ILogger<FriendshipController> _logger;
    private readonly IMediator _mediator;

    public FriendshipController(ILogger<FriendshipController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet("friends")]
    [ProducesResponseType(typeof(PaginatedResponse<FriendDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<FriendDto>>> GetFriends(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _mediator.Send(
            new GetFriendsQuery { UserId = userId, Page = page, PageSize = pageSize, Search = search },
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("requests")]
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<FriendshipDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FriendshipDto>>> GetPendingRequests(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _mediator.Send(new GetPendingRequestsQuery { UserId = userId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("requests/sent")]
    [ProducesResponseType(typeof(IEnumerable<FriendshipDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FriendshipDto>>> GetSentRequests(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _mediator.Send(new GetSentRequestsQuery { UserId = userId }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("request/{targetUserId:int}")]
    [ProducesResponseType(typeof(FriendshipDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FriendshipDto>> SendFriendRequest(int targetUserId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();
        if (userId == targetUserId) return BadRequest(new { message = "Cannot send friend request to yourself" });

        try
        {
            var result = await _mediator.Send(
                new SendFriendRequestCommand { RequesterId = userId, AddresseeId = targetUserId },
                cancellationToken);
            return Created($"/api/friendship/{result.FriendshipId}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("accept/{friendshipId:int}")]
    [ProducesResponseType(typeof(FriendshipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FriendshipDto>> AcceptFriendRequest(int friendshipId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _mediator.Send(
                new AcceptFriendRequestCommand { FriendshipId = friendshipId, UserId = userId },
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Friend request not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("reject/{friendshipId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectFriendRequest(int friendshipId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            await _mediator.Send(
                new RejectFriendRequestCommand { FriendshipId = friendshipId, UserId = userId },
                cancellationToken);
            return Ok(new { message = "Friend request rejected" });
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Friend request not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{friendshipId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteFriendship(int friendshipId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            await _mediator.Send(
                new DeleteFriendshipCommand { FriendshipId = friendshipId, UserId = userId },
                cancellationToken);
            return Ok(new { message = "Friendship removed" });
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Friendship not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("check/{targetUserId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> CheckFriendship(int targetUserId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _mediator.Send(
            new CheckFriendshipQuery { UserId = userId, TargetUserId = targetUserId },
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IEnumerable<FriendDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FriendDto>>> GetSuggestions(
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _mediator.Send(
            new GetFriendSuggestionsQuery { UserId = userId, Limit = limit },
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("analytics/growth")]
    [ProducesResponseType(typeof(NetworkGrowthDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NetworkGrowthDto>> GetNetworkGrowth(
        [FromQuery] int days = 28,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _mediator.Send(
            new GetNetworkGrowthQuery { UserId = userId, Days = days },
            cancellationToken);
        return Ok(result);
    }
}
