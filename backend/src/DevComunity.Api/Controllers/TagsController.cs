using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Common.DTOs;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Tags
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ILogger<TagsController> _logger;

    public TagsController(ILogger<TagsController> logger)
    {
        _logger = logger;
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
        [FromQuery] string sort = "popular", // popular, name, newest
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting tags with search: {Search}", search);

        // TODO: Implement GetTagsQueryHandler
        return Ok(new PaginatedResponse<TagDto>
        {
            Items = new List<TagDto>(),
            Page = page,
            PageSize = pageSize,
            TotalCount = 0
        });
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

        // TODO: Implement GetTagByNameQueryHandler
        return NotFound(new { message = $"Tag '{tagName}' not found" });
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
