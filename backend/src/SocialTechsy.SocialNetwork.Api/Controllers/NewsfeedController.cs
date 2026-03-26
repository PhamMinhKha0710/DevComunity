using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Queries.Posts;
using SocialTechsy.SocialNetwork.Application.Commands.Posts;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NewsfeedController : ControllerBase
{
    private readonly ILogger<NewsfeedController> _logger;
    private readonly IMediator _mediator;

    public NewsfeedController(ILogger<NewsfeedController> logger, IMediator mediator)
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
    [ProducesResponseType(typeof(PaginatedResponse<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<PostDto>>> GetNewsfeed(
        [FromQuery] string? filter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _mediator.Send(
                new GetNewsfeedQuery { UserId = userId, Filter = filter, Page = page, PageSize = pageSize },
                cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load newsfeed for user {UserId} with filter {Filter}", userId, filter);
            return StatusCode(500, new { message = "Failed to load newsfeed" });
        }
    }

    [HttpGet("groups/{groupId:int}")]
    [ProducesResponseType(typeof(PaginatedResponse<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<PostDto>>> GetGroupPosts(
        int groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _mediator.Send(
                new GetGroupPostsQuery { GroupId = groupId, UserId = userId, Page = page, PageSize = pageSize },
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

    [HttpGet("users/{targetUserId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<PostDto>>> GetUserPosts(
        int targetUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetUserPostsQuery { TargetUserId = targetUserId, CurrentUserId = GetCurrentUserId(), Page = page, PageSize = pageSize },
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("posts/{postId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> GetPost(int postId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new GetPostQuery { PostId = postId, CurrentUserId = GetCurrentUserId() > 0 ? GetCurrentUserId() : null },
                cancellationToken);
            if (result == null) return NotFound(new { message = "Post not found" });
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("posts")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PostDto>> CreatePost(
        [FromBody] CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _mediator.Send(
                new CreatePostCommand { AuthorId = userId, Content = request.Content, GroupId = request.GroupId, MediaUrls = request.MediaUrls, Visibility = request.Visibility },
                cancellationToken);
            return Created($"/api/newsfeed/posts/{result.PostId}", result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Group not found")
        {
            return NotFound(new { message = "Group not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("posts/{postId:int}")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> UpdatePost(
        int postId,
        [FromBody] UpdatePostRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _mediator.Send(
                new UpdatePostCommand { PostId = postId, UserId = userId, Content = request.Content, MediaUrls = request.MediaUrls },
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Post not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("posts/{postId:int}/like")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LikePost(int postId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _mediator.Send(
                new LikePostCommand { PostId = postId, UserId = userId },
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Post not found" });
        }
    }

    [HttpDelete("posts/{postId:int}/like")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlikePost(int postId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var result = await _mediator.Send(
                new UnlikePostCommand { PostId = postId, UserId = userId },
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Post not found" });
        }
    }

    [HttpDelete("posts/{postId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(int postId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            await _mediator.Send(new DeletePostCommand { PostId = postId, UserId = userId }, cancellationToken);
            return Ok(new { message = "Post deleted" });
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Post not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
