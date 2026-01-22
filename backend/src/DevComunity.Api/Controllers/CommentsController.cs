using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Commands.Comments;
using DevComunity.Application.CommandHandlers.Comments;
using System.Security.Claims;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Comments on Questions and Answers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ILogger<CommentsController> _logger;
    private readonly CreateQuestionCommentCommandHandler _createQuestionCommentHandler;
    private readonly CreateAnswerCommentCommandHandler _createAnswerCommentHandler;
    private readonly UpdateCommentCommandHandler _updateCommentHandler;
    private readonly DeleteCommentCommandHandler _deleteCommentHandler;

    public CommentsController(
        ILogger<CommentsController> logger,
        CreateQuestionCommentCommandHandler createQuestionCommentHandler,
        CreateAnswerCommentCommandHandler createAnswerCommentHandler,
        UpdateCommentCommandHandler updateCommentHandler,
        DeleteCommentCommandHandler deleteCommentHandler)
    {
        _logger = logger;
        _createQuestionCommentHandler = createQuestionCommentHandler;
        _createAnswerCommentHandler = createAnswerCommentHandler;
        _updateCommentHandler = updateCommentHandler;
        _deleteCommentHandler = deleteCommentHandler;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
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

        var command = new CreateQuestionCommentCommand
        {
            QuestionId = questionId,
            UserId = GetCurrentUserId(),
            Body = request.Body
        };

        var result = await _createQuestionCommentHandler.HandleAsync(command, cancellationToken);
        
        if (result == null)
            return NotFound(new { message = "Question not found" });

        return Created($"/api/comments/{result.CommentId}", result);
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

        var command = new CreateAnswerCommentCommand
        {
            AnswerId = answerId,
            UserId = GetCurrentUserId(),
            Body = request.Body
        };

        var result = await _createAnswerCommentHandler.HandleAsync(command, cancellationToken);
        
        if (result == null)
            return NotFound(new { message = "Answer not found" });

        return Created($"/api/comments/{result.CommentId}", result);
    }

    /// <summary>
    /// Update a comment
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateComment(
        int id,
        [FromBody] UpdateCommentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating comment {CommentId}", id);

        var command = new UpdateCommentCommand
        {
            CommentId = id,
            UserId = GetCurrentUserId(),
            Body = request.Body
        };

        var success = await _updateCommentHandler.HandleAsync(command, cancellationToken);
        
        if (!success)
            return NotFound(new { message = "Comment not found or you don't have permission to edit it" });

        return Ok(new { message = "Comment updated successfully" });
    }

    /// <summary>
    /// Delete a comment
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting comment {CommentId}", id);

        var command = new DeleteCommentCommand
        {
            CommentId = id,
            UserId = GetCurrentUserId()
        };

        var success = await _deleteCommentHandler.HandleAsync(command, cancellationToken);
        
        if (!success)
            return NotFound(new { message = "Comment not found or you don't have permission to delete it" });

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
