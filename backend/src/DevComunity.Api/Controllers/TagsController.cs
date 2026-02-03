using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Queries.Tags;
using DevComunity.Application.Queries.Questions;
using DevComunity.Application.QueryHandlers.Tags;
using DevComunity.Application.QueryHandlers.Questions;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using System.Security.Claims;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Tags
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ILogger<TagsController> _logger;
    private readonly GetTagsQueryHandler _getTagsHandler;
    private readonly GetTagByNameQueryHandler _getTagByNameHandler;
    private readonly GetQuestionsQueryHandler _getQuestionsHandler;
    private readonly ITagPreferenceRepository _tagPreferenceRepository;
    private readonly ITagRepository _tagRepository;

    public TagsController(
        ILogger<TagsController> logger,
        GetTagsQueryHandler getTagsHandler,
        GetTagByNameQueryHandler getTagByNameHandler,
        GetQuestionsQueryHandler getQuestionsHandler,
        ITagPreferenceRepository tagPreferenceRepository,
        ITagRepository tagRepository)
    {
        _logger = logger;
        _getTagsHandler = getTagsHandler;
        _getTagByNameHandler = getTagByNameHandler;
        _getQuestionsHandler = getQuestionsHandler;
        _tagPreferenceRepository = tagPreferenceRepository;
        _tagRepository = tagRepository;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get all tags with usage count
    /// </summary>
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

        var query = new GetTagsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Sort = sortBy
        };

        var result = await _getTagsHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get tag by name
    /// </summary>
    [HttpGet("{tagName}")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDto>> GetTag(string tagName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tag: {TagName}", tagName);

        var query = new GetTagByNameQuery { TagName = tagName };
        var result = await _getTagByNameHandler.HandleAsync(query, cancellationToken);

        if (result == null)
            return NotFound(new { message = $"Tag '{tagName}' not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get questions by tag
    /// </summary>
    [HttpGet("{tagName}/questions")]
    [ProducesResponseType(typeof(PaginatedResponse<QuestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<QuestionDto>>> GetQuestionsByTag(
        string tagName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string sort = "newest",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting questions for tag: {TagName}", tagName);

        var query = new GetQuestionsQuery
        {
            Page = page,
            PageSize = pageSize,
            Tag = tagName,
            Sort = sort
        };

        var result = await _getQuestionsHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    // ========== TAG PREFERENCES ENDPOINTS ==========

    /// <summary>
    /// Get current user's tag preferences
    /// </summary>
    [HttpGet("preferences")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<TagPreferenceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TagPreferenceDto>>> GetTagPreferences(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("Getting tag preferences for user {UserId}", userId);

        var preferences = await _tagPreferenceRepository.GetUserPreferencesAsync(userId, cancellationToken);
        
        var result = preferences.Select(p => new TagPreferenceDto
        {
            TagId = p.TagId,
            TagName = p.Tag?.TagName ?? "",
            IsFollowed = p.IsFollowed,
            IsIgnored = p.IsIgnored
        });

        return Ok(result);
    }

    /// <summary>
    /// Get followed tags for current user
    /// </summary>
    [HttpGet("preferences/followed")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<TagPreferenceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TagPreferenceDto>>> GetFollowedTags(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var preferences = await _tagPreferenceRepository.GetFollowedTagsAsync(userId, cancellationToken);
        
        var result = preferences.Select(p => new TagPreferenceDto
        {
            TagId = p.TagId,
            TagName = p.Tag?.TagName ?? "",
            IsFollowed = true,
            IsIgnored = false
        });

        return Ok(result);
    }

    /// <summary>
    /// Follow a tag
    /// </summary>
    [HttpPost("{tagId:int}/follow")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FollowTag(int tagId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        // Verify tag exists
        var tag = await _tagRepository.GetByIdAsync(tagId, cancellationToken);
        if (tag == null)
            return NotFound(new { message = "Tag not found" });

        _logger.LogInformation("User {UserId} following tag {TagId}", userId, tagId);

        var preference = new TagPreference
        {
            UserId = userId,
            TagId = tagId,
            IsFollowed = true,
            IsIgnored = false
        };

        await _tagPreferenceRepository.UpsertAsync(preference, cancellationToken);
        return Ok(new { message = $"Now following tag: {tag.TagName}" });
    }

    /// <summary>
    /// Ignore a tag
    /// </summary>
    [HttpPost("{tagId:int}/ignore")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IgnoreTag(int tagId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var tag = await _tagRepository.GetByIdAsync(tagId, cancellationToken);
        if (tag == null)
            return NotFound(new { message = "Tag not found" });

        _logger.LogInformation("User {UserId} ignoring tag {TagId}", userId, tagId);

        var preference = new TagPreference
        {
            UserId = userId,
            TagId = tagId,
            IsFollowed = false,
            IsIgnored = true
        };

        await _tagPreferenceRepository.UpsertAsync(preference, cancellationToken);
        return Ok(new { message = $"Now ignoring tag: {tag.TagName}" });
    }

    /// <summary>
    /// Remove tag preference (unfollow/unignore)
    /// </summary>
    [HttpDelete("{tagId:int}/preference")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePreference(int tagId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} removing preference for tag {TagId}", userId, tagId);

        var deleted = await _tagPreferenceRepository.DeleteAsync(userId, tagId, cancellationToken);
        
        if (!deleted)
            return NotFound(new { message = "No preference found for this tag" });

        return Ok(new { message = "Tag preference removed" });
    }
}

/// <summary>
/// DTO for tag preference response
/// </summary>
public class TagPreferenceDto
{
    public int TagId { get; set; }
    public string TagName { get; set; } = "";
    public bool IsFollowed { get; set; }
    public bool IsIgnored { get; set; }
}
