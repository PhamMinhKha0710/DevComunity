using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Saved Items (bookmarks)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SavedItemsController : ControllerBase
{
    private readonly ILogger<SavedItemsController> _logger;

    public SavedItemsController(ILogger<SavedItemsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get user's saved items
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSavedItems(
        [FromQuery] string? type = null, // "question", "answer", or null for all
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting saved items, type: {Type}", type);

        // TODO: Implement GetSavedItemsQueryHandler
        return Ok(new { items = new List<object>(), page, pageSize, totalCount = 0 });
    }

    /// <summary>
    /// Save a question
    /// </summary>
    [HttpPost("questions/{questionId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> SaveQuestion(int questionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Saving question {QuestionId}", questionId);

        // TODO: Implement SaveQuestionCommandHandler
        return Created("", new { message = "Question saved" });
    }

    /// <summary>
    /// Remove saved question
    /// </summary>
    [HttpDelete("questions/{questionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnsaveQuestion(int questionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unsaving question {QuestionId}", questionId);

        // TODO: Implement UnsaveQuestionCommandHandler
        return NoContent();
    }

    /// <summary>
    /// Save an answer
    /// </summary>
    [HttpPost("answers/{answerId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> SaveAnswer(int answerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Saving answer {AnswerId}", answerId);

        // TODO: Implement SaveAnswerCommandHandler
        return Created("", new { message = "Answer saved" });
    }

    /// <summary>
    /// Remove saved answer
    /// </summary>
    [HttpDelete("answers/{answerId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnsaveAnswer(int answerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unsaving answer {AnswerId}", answerId);

        // TODO: Implement UnsaveAnswerCommandHandler
        return NoContent();
    }
}
