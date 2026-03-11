using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Commands.SavedItems;
using SocialTechsy.SocialNetwork.Application.Queries.SavedItems;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.SavedItems;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SavedItemsController : ControllerBase
{
    private readonly ILogger<SavedItemsController> _logger;
    private readonly IMediator _mediator;

    public SavedItemsController(ILogger<SavedItemsController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSavedItems(
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting saved items, type: {Type}", type);

        var result = await _mediator.Send(new GetSavedItemsQuery
        {
            UserId = GetCurrentUserId(),
            Type = type,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(new { items = result.Items, page, pageSize, totalCount = result.TotalCount });
    }

    [HttpPost("questions/{questionId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveQuestion(int questionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Saving question {QuestionId}", questionId);

        var success = await _mediator.Send(new SaveQuestionCommand
        {
            UserId = GetCurrentUserId(),
            QuestionId = questionId
        }, cancellationToken);

        if (!success)
            return NotFound(new { message = "Question not found" });

        return Created("", new { message = "Question saved" });
    }

    [HttpDelete("questions/{questionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnsaveQuestion(int questionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unsaving question {QuestionId}", questionId);

        await _mediator.Send(new UnsaveQuestionCommand
        {
            UserId = GetCurrentUserId(),
            QuestionId = questionId
        }, cancellationToken);

        return NoContent();
    }

    [HttpPost("answers/{answerId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveAnswer(int answerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Saving answer {AnswerId}", answerId);

        var success = await _mediator.Send(new SaveAnswerCommand
        {
            UserId = GetCurrentUserId(),
            AnswerId = answerId
        }, cancellationToken);

        if (!success)
            return NotFound(new { message = "Answer not found" });

        return Created("", new { message = "Answer saved" });
    }

    [HttpDelete("answers/{answerId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnsaveAnswer(int answerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unsaving answer {AnswerId}", answerId);

        await _mediator.Send(new UnsaveAnswerCommand
        {
            UserId = GetCurrentUserId(),
            AnswerId = answerId
        }, cancellationToken);

        return NoContent();
    }
}
