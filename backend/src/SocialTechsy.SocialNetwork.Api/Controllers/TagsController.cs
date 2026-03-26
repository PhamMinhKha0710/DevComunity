using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Tag;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Queries.Tags;
using SocialTechsy.SocialNetwork.Application.Queries.Questions;
using SocialTechsy.SocialNetwork.Application.Commands.Tags;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ILogger<TagsController> _logger;
    private readonly IMediator _mediator;

    public TagsController(
        ILogger<TagsController> logger,
        IMediator mediator)
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
    [ProducesResponseType(typeof(PaginatedResponse<TagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<TagDto>>> GetTags(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 36,
        [FromQuery] string sortBy = "popular",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting tags with search: {Search}", search);

        var result = await _mediator.Send(new GetTagsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Sort = sortBy
        }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{tagName}")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDto>> GetTag(string tagName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tag: {TagName}", tagName);

        var result = await _mediator.Send(new GetTagByNameQuery { TagName = tagName }, cancellationToken);

        if (result == null)
            return NotFound(new { message = $"Tag '{tagName}' not found" });

        return Ok(result);
    }

    [HttpGet("{tagName}/questions")]
    [ProducesResponseType(typeof(PaginatedResponse<QuestionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<QuestionSummaryDto>>> GetQuestionsByTag(
        string tagName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string sort = "newest",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting questions for tag: {TagName}", tagName);

        var result = await _mediator.Send(new GetQuestionsQuery
        {
            Page = page,
            PageSize = pageSize,
            Tag = tagName,
            Sort = sort
        }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("preferences")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<TagPreferenceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TagPreferenceDto>>> GetTagPreferences(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("Getting tag preferences for user {UserId}", userId);

        var result = await _mediator.Send(new GetUserTagPreferencesQuery { UserId = userId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("preferences/followed")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<TagPreferenceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TagPreferenceDto>>> GetFollowedTags(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _mediator.Send(new GetFollowedTagsQuery { UserId = userId }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{tagId:int}/follow")]
    [Authorize]
    [ProducesResponseType(typeof(TagPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TagPreferenceDto>> FollowTag(int tagId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} following tag {TagId}", userId, tagId);

        var result = await _mediator.Send(new FollowTagCommand
        {
            TagId = tagId,
            UserId = userId
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{tagId:int}/ignore")]
    [Authorize]
    [ProducesResponseType(typeof(TagPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TagPreferenceDto>> IgnoreTag(int tagId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} ignoring tag {TagId}", userId, tagId);

        var result = await _mediator.Send(new IgnoreTagCommand
        {
            TagId = tagId,
            UserId = userId
        }, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{tagId:int}/preference")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePreference(int tagId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} removing preference for tag {TagId}", userId, tagId);

        var deleted = await _mediator.Send(new RemoveTagPreferenceCommand
        {
            TagId = tagId,
            UserId = userId
        }, cancellationToken);

        if (!deleted)
            return NotFound(new { message = "No preference found for this tag" });

        return Ok(new { message = "Tag preference removed" });
    }
}
