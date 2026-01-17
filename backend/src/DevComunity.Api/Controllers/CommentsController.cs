using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Common.DTOs;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Comments on Questions and Answers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(ILogger<CommentsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Add comment to a question
    /// </summary>
    [HttpPost("question/{questionId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> AddQuestionComment(
        int questionId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding comment to question {QuestionId}", questionId);

        // TODO: Implement CreateQuestionCommentCommandHandler
        return Created("", new CommentDto
        {
            CommentId = 1,
            Body = request.Body,
            CreatedDate = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Add comment to an answer
    /// </summary>
    [HttpPost("answer/{answerId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> AddAnswerComment(
        int answerId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding comment to answer {AnswerId}", answerId);

        // TODO: Implement CreateAnswerCommentCommandHandler
        return Created("", new CommentDto
        {
            CommentId = 1,
            Body = request.Body,
            CreatedDate = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Update a comment
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateComment(
        int id,
        [FromBody] UpdateCommentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating comment {CommentId}", id);

        // TODO: Implement UpdateCommentCommandHandler
        return Ok(new { message = "Comment updated" });
    }

    /// <summary>
    /// Delete a comment
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting comment {CommentId}", id);

        // TODO: Implement DeleteCommentCommandHandler
        return NoContent();
    }
}

public class CreateCommentRequest
{
    public string Body { get; set; } = null!;
}

public class UpdateCommentRequest
{
    public string Body { get; set; } = null!;
}
