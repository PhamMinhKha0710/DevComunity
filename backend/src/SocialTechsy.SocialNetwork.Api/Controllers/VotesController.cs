using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Commands.Votes;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Enums;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VotesController : ControllerBase
{
    private readonly ILogger<VotesController> _logger;
    private readonly IMediator _mediator;
    private readonly ILikeService? _likeService;

    public VotesController(
        ILogger<VotesController> logger,
        IMediator mediator,
        ILikeService? likeService = null)
    {
        _logger = logger;
        _mediator = mediator;
        _likeService = likeService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpPost("post/{postId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VotePost(
        int postId,
        [FromBody] VoteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        if (!Enum.TryParse<VoteType>(request.VoteType, ignoreCase: true, out var voteType))
            return BadRequest(new { message = "Invalid vote type. Must be 'up' or 'down'." });

        _logger.LogInformation("User {UserId} voting on post {PostId}: {VoteType}", userId, postId, voteType);

        if (_likeService == null)
            return StatusCode(503, new { message = "Like service not available" });

        // Only upvote is supported for posts
        if (voteType != VoteType.Up)
            return BadRequest(new { message = "Only 'up' vote is supported for posts" });

        var likeCount = await _likeService.LikeAsync("post", postId, userId);
        return Ok(new { score = likeCount, userVote = "up" });
    }

    [HttpDelete("post/{postId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemovePostVote(int postId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("User {UserId} removing vote from post {PostId}", userId, postId);

        if (_likeService == null)
            return StatusCode(503, new { message = "Like service not available" });

        var likeCount = await _likeService.UnlikeAsync("post", postId, userId);
        return Ok(new { score = likeCount });
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

        if (!Enum.TryParse<VoteType>(request.VoteType, ignoreCase: true, out var voteType))
            return BadRequest(new { message = "Invalid vote type. Must be 'up' or 'down'." });

        _logger.LogInformation("User {UserId} voting on question {QuestionId}: {VoteType}", userId, questionId, voteType);

        var result = await _mediator.Send(new VoteQuestionCommand
        {
            UserId = userId,
            QuestionId = questionId,
            VoteType = voteType
        }, cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

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

        if (!Enum.TryParse<VoteType>(request.VoteType, ignoreCase: true, out var voteType))
            return BadRequest(new { message = "Invalid vote type. Must be 'up' or 'down'." });

        _logger.LogInformation("User {UserId} voting on answer {AnswerId}: {VoteType}", userId, answerId, voteType);

        var result = await _mediator.Send(new VoteAnswerCommand
        {
            UserId = userId,
            AnswerId = answerId,
            VoteType = voteType
        }, cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

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
