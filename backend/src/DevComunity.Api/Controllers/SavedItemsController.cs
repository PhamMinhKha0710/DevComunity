using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Commands.SavedItems;
using DevComunity.Application.Queries.SavedItems;
using DevComunity.Application.CommandHandlers.SavedItems;
using DevComunity.Application.QueryHandlers.SavedItems;
using System.Security.Claims;

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
    private readonly GetSavedItemsQueryHandler _getSavedItemsHandler;
    private readonly SaveQuestionCommandHandler _saveQuestionHandler;
    private readonly UnsaveQuestionCommandHandler _unsaveQuestionHandler;
    private readonly SaveAnswerCommandHandler _saveAnswerHandler;
    private readonly UnsaveAnswerCommandHandler _unsaveAnswerHandler;

    public SavedItemsController(
        ILogger<SavedItemsController> logger,
        GetSavedItemsQueryHandler getSavedItemsHandler,
        SaveQuestionCommandHandler saveQuestionHandler,
        UnsaveQuestionCommandHandler unsaveQuestionHandler,
        SaveAnswerCommandHandler saveAnswerHandler,
        UnsaveAnswerCommandHandler unsaveAnswerHandler)
    {
        _logger = logger;
        _getSavedItemsHandler = getSavedItemsHandler;
        _saveQuestionHandler = saveQuestionHandler;
        _unsaveQuestionHandler = unsaveQuestionHandler;
        _saveAnswerHandler = saveAnswerHandler;
        _unsaveAnswerHandler = unsaveAnswerHandler;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    /// <summary>
    /// Get user's saved items
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSavedItems(
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting saved items, type: {Type}", type);

        var query = new GetSavedItemsQuery
        {
            UserId = GetCurrentUserId(),
            Type = type,
            Page = page,
            PageSize = pageSize
        };

        var (items, totalCount) = await _getSavedItemsHandler.HandleAsync(query, cancellationToken);
        
        return Ok(new { items, page, pageSize, totalCount });
    }

    /// <summary>
    /// Save a question
    /// </summary>
    [HttpPost("questions/{questionId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveQuestion(int questionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Saving question {QuestionId}", questionId);

        var command = new SaveQuestionCommand
        {
            UserId = GetCurrentUserId(),
            QuestionId = questionId
        };

        var success = await _saveQuestionHandler.HandleAsync(command, cancellationToken);
        
        if (!success)
            return NotFound(new { message = "Question not found" });

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

        var command = new UnsaveQuestionCommand
        {
            UserId = GetCurrentUserId(),
            QuestionId = questionId
        };

        await _unsaveQuestionHandler.HandleAsync(command, cancellationToken);
        
        return NoContent();
    }

    /// <summary>
    /// Save an answer
    /// </summary>
    [HttpPost("answers/{answerId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveAnswer(int answerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Saving answer {AnswerId}", answerId);

        var command = new SaveAnswerCommand
        {
            UserId = GetCurrentUserId(),
            AnswerId = answerId
        };

        var success = await _saveAnswerHandler.HandleAsync(command, cancellationToken);
        
        if (!success)
            return NotFound(new { message = "Answer not found" });

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

        var command = new UnsaveAnswerCommand
        {
            UserId = GetCurrentUserId(),
            AnswerId = answerId
        };

        await _unsaveAnswerHandler.HandleAsync(command, cancellationToken);
        
        return NoContent();
    }
}
