using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Application.Queries.Questions;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly ILogger<QuestionsController> _logger;
    private readonly IMediator _mediator;
    private readonly IViewService? _viewService;
    private readonly IActivityLogService? _activityLog;

    public QuestionsController(
        ILogger<QuestionsController> logger,
        IMediator mediator,
        IViewService? viewService = null,
        IActivityLogService? activityLog = null)
    {
        _logger = logger;
        _mediator = mediator;
        _viewService = viewService;
        _activityLog = activityLog;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<QuestionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<QuestionSummaryDto>>> GetQuestions(
        [FromQuery] GetQuestionsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting questions with params: {@Query}", query);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(QuestionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionDetailDto>> GetQuestion(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting question with ID: {QuestionId}", id);

        var result = await _mediator.Send(new GetQuestionByIdQuery { QuestionId = id, CurrentUserId = GetCurrentUserId() }, cancellationToken);

        if (result == null)
            return NotFound(new { message = $"Question with ID {id} not found" });

        var userId = GetCurrentUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (_viewService != null)
        {
            _ = _viewService.IncrementViewAsync(id, userId > 0 ? userId.ToString() : null, ip);
        }
        else
        {
            _ = _mediator.Send(new IncrementViewCountCommand { QuestionId = id }, cancellationToken);
        }

        if (_activityLog != null)
        {
            var ua = Request.Headers.UserAgent.ToString();
            _ = _activityLog.LogViewAsync(id, userId > 0 ? userId : null, ip, ua);
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(QuestionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<QuestionDto>> CreateQuestion(
        [FromBody] CreateQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        command.UserId = userId;
        _logger.LogInformation("User {UserId} creating question: {Title}", userId, command.Title);

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetQuestion),
            new { id = result.QuestionId },
            result);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateQuestion(
        int id,
        [FromBody] UpdateQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        command.QuestionId = id;
        command.UserId = userId;

        _logger.LogInformation("User {UserId} updating question {QuestionId}", userId, id);

        await _mediator.Send(command, cancellationToken);

        return Ok(new { message = "Question updated successfully" });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteQuestion(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("User {UserId} deleting question {QuestionId}", userId, id);

        await _mediator.Send(new DeleteQuestionCommand { QuestionId = id, UserId = userId }, cancellationToken);

        return NoContent();
    }
}
