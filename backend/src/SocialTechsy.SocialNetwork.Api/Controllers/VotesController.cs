using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Commands.Votes;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Api.Hubs;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VotesController : ControllerBase
{
    private readonly ILogger<VotesController> _logger;
    private readonly IMediator _mediator;
    private readonly IHubContext<NotificationHub> _hubContext;

    public VotesController(
        ILogger<VotesController> logger,
        IMediator mediator,
        IHubContext<NotificationHub> hubContext)
    {
        _logger = logger;
        _mediator = mediator;
        _hubContext = hubContext;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpPost("question/{questionId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VoteQuestion(
        int questionId,
        [FromBody] VoteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("User {UserId} voting on question {QuestionId}: {VoteType}", userId, questionId, request.VoteType);

        var result = await _mediator.Send(new VoteQuestionCommand
        {
            UserId = userId,
            QuestionId = questionId,
            VoteType = request.VoteType
        }, cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        if (result.CreatedNotification != null)
        {
            try
            {
                await _hubContext.Clients.Group($"user_{result.CreatedNotification.UserId}")
                    .SendAsync("ReceiveNotification", result.CreatedNotification, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending upvote notification");
            }
        }

        return Ok(new { score = result.Score, userVote = result.UserVote });
    }

    [HttpPost("answer/{answerId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VoteAnswer(
        int answerId,
        [FromBody] VoteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("User {UserId} voting on answer {AnswerId}: {VoteType}", userId, answerId, request.VoteType);

        var result = await _mediator.Send(new VoteAnswerCommand
        {
            UserId = userId,
            AnswerId = answerId,
            VoteType = request.VoteType
        }, cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        if (result.CreatedNotification != null)
        {
            try
            {
                await _hubContext.Clients.Group($"user_{result.CreatedNotification.UserId}")
                    .SendAsync("ReceiveNotification", result.CreatedNotification, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending answer upvote notification");
            }
        }

        return Ok(new { score = result.Score, userVote = result.UserVote });
    }

    [HttpDelete("question/{questionId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveQuestionVote(int questionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("User {UserId} removing vote from question {QuestionId}", userId, questionId);

        var result = await _mediator.Send(new RemoveVoteCommand { UserId = userId, QuestionId = questionId }, cancellationToken);
        return Ok(new { score = result.Score });
    }

    [HttpDelete("answer/{answerId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveAnswerVote(int answerId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("User {UserId} removing vote from answer {AnswerId}", userId, answerId);

        var result = await _mediator.Send(new RemoveVoteCommand { UserId = userId, AnswerId = answerId }, cancellationToken);
        return Ok(new { score = result.Score });
    }
}

public class VoteRequest
{
    public string VoteType { get; set; } = null!;
}
