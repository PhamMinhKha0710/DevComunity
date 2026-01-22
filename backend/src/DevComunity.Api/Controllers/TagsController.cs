using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Queries.Tags;
using DevComunity.Application.QueryHandlers.Tags;

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

    public TagsController(
        ILogger<TagsController> logger,
        GetTagsQueryHandler getTagsHandler,
        GetTagByNameQueryHandler getTagByNameHandler)
    {
        _logger = logger;
        _getTagsHandler = getTagsHandler;
        _getTagByNameHandler = getTagByNameHandler;
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
        [FromQuery] string sort = "popular",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting tags with search: {Search}", search);

        var query = new GetTagsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Sort = sort
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
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting questions for tag: {TagName}", tagName);

        // TODO: Implement GetQuestionsByTagQueryHandler
        return Ok(new PaginatedResponse<QuestionDto>
        {
            Items = new List<QuestionDto>(),
            Page = page,
            PageSize = pageSize,
            TotalCount = 0
        });
    }
}
