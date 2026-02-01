using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Commands.Votes;
using DevComunity.Application.CommandHandlers.Votes;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using DevComunity.Api.Hubs;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Voting on Questions and Answers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VotesController : ControllerBase
{
    private readonly ILogger<VotesController> _logger;
    private readonly VoteQuestionCommandHandler _voteQuestionHandler;
    private readonly VoteAnswerCommandHandler _voteAnswerHandler;
    private readonly RemoveVoteCommandHandler _removeVoteHandler;
    private readonly IHubContext<NotificationHub> _hubContext;

    public VotesController(
        ILogger<VotesController> logger,
        VoteQuestionCommandHandler voteQuestionHandler,
        VoteAnswerCommandHandler voteAnswerHandler,
        RemoveVoteCommandHandler removeVoteHandler,
        IHubContext<NotificationHub> hubContext)
    {
        _logger = logger;
        _voteQuestionHandler = voteQuestionHandler;
        _voteAnswerHandler = voteAnswerHandler;
        _removeVoteHandler = removeVoteHandler;
        _hubContext = hubContext;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Vote on a question
    /// </summary>
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

        var command = new VoteQuestionCommand
        {
            UserId = userId,
            QuestionId = questionId,
            VoteType = request.VoteType
        };

        var result = await _voteQuestionHandler.HandleAsync(command, cancellationToken);

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

    /// <summary>
    /// Vote on an answer
    /// </summary>
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

        var command = new VoteAnswerCommand
        {
            UserId = userId,
            AnswerId = answerId,
            VoteType = request.VoteType
        };

        var result = await _voteAnswerHandler.HandleAsync(command, cancellationToken);

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

    /// <summary>
    /// Remove vote from a question
    /// </summary>
    [HttpDelete("question/{questionId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveQuestionVote(int questionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("User {UserId} removing vote from question {QuestionId}", userId, questionId);

        var command = new RemoveVoteCommand
        {
            UserId = userId,
            QuestionId = questionId
        };

        var result = await _removeVoteHandler.HandleAsync(command, cancellationToken);
        return Ok(new { score = result.Score });
    }

    /// <summary>
    /// Remove vote from an answer
    /// </summary>
    [HttpDelete("answer/{answerId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveAnswerVote(int answerId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("User {UserId} removing vote from answer {AnswerId}", userId, answerId);

        var command = new RemoveVoteCommand
        {
            UserId = userId,
            AnswerId = answerId
        };

        var result = await _removeVoteHandler.HandleAsync(command, cancellationToken);
        return Ok(new { score = result.Score });
    }
}

public class VoteRequest
{
    public string VoteType { get; set; } = null!; // "up" or "down"
}
