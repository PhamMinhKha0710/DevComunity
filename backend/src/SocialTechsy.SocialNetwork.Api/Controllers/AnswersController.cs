using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Commands.Answers;
using SocialTechsy.SocialNetwork.Application.Queries.Answers;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnswersController : ControllerBase
{
    private readonly ILogger<AnswersController> _logger;
    private readonly IMediator _mediator;

    public AnswersController(ILogger<AnswersController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("question/{questionId:int}")]
    [ProducesResponseType(typeof(PaginatedResponse<AnswerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<AnswerDto>>> GetAnswersByQuestion(
        int questionId,
        [FromQuery] string sort = "votes",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting answers for question {QuestionId}", questionId);

        var result = await _mediator.Send(new GetAnswersByQuestionQuery
        {
            QuestionId = questionId,
            CurrentUserId = GetCurrentUserId(),
            Sort = sort,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AnswerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnswerDto>> GetAnswer(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting answer {AnswerId}", id);

        var result = await _mediator.Send(new GetAnswerByIdQuery { AnswerId = id }, cancellationToken);

        if (result == null)
            return NotFound(new { message = $"Answer with ID {id} not found" });

        return Ok(result);
    }

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
        var result = await _mediator.Send(command, cancellationToken);

        if (result == null)
            return NotFound(new { message = "Question not found" });

        return CreatedAtAction(nameof(GetAnswer), new { id = result.AnswerId }, result);
    }

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

        var success = await _mediator.Send(command, cancellationToken);

        if (!success)
            return NotFound();

        return Ok(new { message = "Answer updated successfully" });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAnswer(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting answer {AnswerId}", id);

        var success = await _mediator.Send(new DeleteAnswerCommand { AnswerId = id, UserId = GetCurrentUserId() }, cancellationToken);

        if (!success)
            return NotFound();

        return NoContent();
    }

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

        var result = await _mediator.Send(new AcceptAnswerCommand
        {
            AnswerId = id,
            QuestionId = questionId,
            UserId = GetCurrentUserId()
        }, cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message ?? "Answer accepted successfully" });
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub")
            ?? User.FindFirst("userId");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }
}
