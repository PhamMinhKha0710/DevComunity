using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Commands.Comments;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ILogger<CommentsController> _logger;
    private readonly IMediator _mediator;

    public CommentsController(ILogger<CommentsController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

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

        var result = await _mediator.Send(new CreateQuestionCommentCommand
        {
            QuestionId = questionId,
            UserId = GetCurrentUserId(),
            Body = request.Body
        }, cancellationToken);

        if (result == null)
            return NotFound(new { message = "Question not found" });

        return Created($"/api/comments/{result.CommentId}", result);
    }

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

        var result = await _mediator.Send(new CreateAnswerCommentCommand
        {
            AnswerId = answerId,
            UserId = GetCurrentUserId(),
            Body = request.Body
        }, cancellationToken);

        if (result == null)
            return NotFound(new { message = "Answer not found" });

        return Created($"/api/comments/{result.CommentId}", result);
    }

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

        var success = await _mediator.Send(new UpdateCommentCommand
        {
            CommentId = id,
            UserId = GetCurrentUserId(),
            Body = request.Body
        }, cancellationToken);

        if (!success)
            return NotFound(new { message = "Comment not found or you don't have permission to edit it" });

        return Ok(new { message = "Comment updated successfully" });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting comment {CommentId}", id);

        var success = await _mediator.Send(new DeleteCommentCommand
        {
            CommentId = id,
            UserId = GetCurrentUserId()
        }, cancellationToken);

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
