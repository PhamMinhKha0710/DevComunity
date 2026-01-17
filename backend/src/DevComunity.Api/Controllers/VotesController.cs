using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Voting on Questions and Answers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VotesController : ControllerBase
{
    private readonly ILogger<VotesController> _logger;

    public VotesController(ILogger<VotesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Vote on a question
    /// </summary>
    [HttpPost("question/{questionId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VoteQuestion(
        int questionId,
        [FromBody] VoteRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Voting on question {QuestionId}: {VoteType}", questionId, request.VoteType);

        // TODO: Implement VoteQuestionCommandHandler
        return Ok(new { score = 0, userVote = request.VoteType });
    }

    /// <summary>
    /// Vote on an answer
    /// </summary>
    [HttpPost("answer/{answerId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VoteAnswer(
        int answerId,
        [FromBody] VoteRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Voting on answer {AnswerId}: {VoteType}", answerId, request.VoteType);

        // TODO: Implement VoteAnswerCommandHandler
        return Ok(new { score = 0, userVote = request.VoteType });
    }

    /// <summary>
    /// Remove vote from a question
    /// </summary>
    [HttpDelete("question/{questionId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveQuestionVote(int questionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing vote from question {QuestionId}", questionId);

        // TODO: Implement RemoveVoteCommandHandler
        return Ok(new { score = 0 });
    }

    /// <summary>
    /// Remove vote from an answer
    /// </summary>
    [HttpDelete("answer/{answerId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveAnswerVote(int answerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing vote from answer {AnswerId}", answerId);

        // TODO: Implement RemoveVoteCommandHandler
        return Ok(new { score = 0 });
    }
}

public class VoteRequest
{
    public string VoteType { get; set; } = null!; // "up" or "down"
}
