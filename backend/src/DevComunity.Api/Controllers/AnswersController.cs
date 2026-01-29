using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Commands.Answers;
using DevComunity.Application.CommandHandlers.Answers;
using DevComunity.Application.Queries.Answers;
using DevComunity.Application.QueryHandlers.Answers;
using DevComunity.Application.Common.DTOs;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Answers - Uses CQRS pattern
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnswersController : ControllerBase
{
    private readonly ILogger<AnswersController> _logger;
    private readonly CreateAnswerCommandHandler _createHandler;
    private readonly UpdateAnswerCommandHandler _updateHandler;
    private readonly DeleteAnswerCommandHandler _deleteHandler;
    private readonly AcceptAnswerCommandHandler _acceptHandler;
    private readonly GetAnswerByIdQueryHandler _getAnswerByIdHandler;
    private readonly GetAnswersByQuestionQueryHandler _getAnswersByQuestionHandler;

    public AnswersController(
        ILogger<AnswersController> logger,
        CreateAnswerCommandHandler createHandler,
        UpdateAnswerCommandHandler updateHandler,
        DeleteAnswerCommandHandler deleteHandler,
        AcceptAnswerCommandHandler acceptHandler,
        GetAnswerByIdQueryHandler getAnswerByIdHandler,
        GetAnswersByQuestionQueryHandler getAnswersByQuestionHandler)
    {
        _logger = logger;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _acceptHandler = acceptHandler;
        _getAnswerByIdHandler = getAnswerByIdHandler;
        _getAnswersByQuestionHandler = getAnswersByQuestionHandler;
    }

    /// <summary>
    /// Get answers for a question
    /// </summary>
    [HttpGet("question/{questionId:int}")]
    [ProducesResponseType(typeof(List<AnswerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AnswerDto>>> GetAnswersByQuestion(
        int questionId,
        [FromQuery] string sort = "votes",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting answers for question {QuestionId}", questionId);

        var query = new GetAnswersByQuestionQuery
        {
            QuestionId = questionId,
            Sort = sort
        };

        var result = await _getAnswersByQuestionHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get answer by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AnswerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnswerDto>> GetAnswer(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting answer {AnswerId}", id);

        var query = new GetAnswerByIdQuery { AnswerId = id };
        var result = await _getAnswerByIdHandler.HandleAsync(query, cancellationToken);

        if (result == null)
            return NotFound(new { message = $"Answer with ID {id} not found" });

        return Ok(result);
    }

    /// <summary>
    /// Create a new answer
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(AnswerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnswerDto>> CreateAnswer(
        [FromBody] CreateAnswerCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Creating answer for question {QuestionId}", command.QuestionId);

        command.UserId = GetCurrentUserId();

        var result = await _createHandler.HandleAsync(command, cancellationToken);

        if (result == null)
            return NotFound(new { message = "Question not found" });

        return CreatedAtAction(nameof(GetAnswer), new { id = result.AnswerId }, result);
    }

    /// <summary>
    /// Update an answer
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateAnswer(
        int id,
        [FromBody] UpdateAnswerCommand command,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        command.AnswerId = id;
        command.UserId = GetCurrentUserId();

        _logger.LogInformation("Updating answer {AnswerId}", id);

        var success = await _updateHandler.HandleAsync(command, cancellationToken);

        if (!success)
            return NotFound();

        return Ok(new { message = "Answer updated successfully" });
    }

    /// <summary>
    /// Delete an answer
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAnswer(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting answer {AnswerId}", id);

        var command = new DeleteAnswerCommand { AnswerId = id, UserId = GetCurrentUserId() };

        var success = await _deleteHandler.HandleAsync(command, cancellationToken);

        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Accept an answer (for question author only)
    /// </summary>
    [HttpPost("{id:int}/accept")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AcceptAnswer(
        int id,
        [FromQuery] int questionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Accepting answer {AnswerId} for question {QuestionId}", id, questionId);

        var command = new AcceptAnswerCommand
        {
            AnswerId = id,
            QuestionId = questionId,
            UserId = GetCurrentUserId()
        };

        var success = await _acceptHandler.HandleAsync(command, cancellationToken);

        if (!success)
            return NotFound();

        return Ok(new { message = "Answer accepted successfully" });
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub")
            ?? User.FindFirst("userId");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }
}
