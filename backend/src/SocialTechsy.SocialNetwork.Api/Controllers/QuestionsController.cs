using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Questions;
using SocialTechsy.SocialNetwork.Application.Queries.Questions;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Questions;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

/// <summary>
/// API Controller for Questions - Uses CQRS pattern
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly ILogger<QuestionsController> _logger;
    private readonly CreateQuestionCommandHandler _createHandler;
    private readonly UpdateQuestionCommandHandler _updateHandler;
    private readonly DeleteQuestionCommandHandler _deleteHandler;
    private readonly GetQuestionsQueryHandler _getQuestionsHandler;
    private readonly GetQuestionByIdQueryHandler _getQuestionByIdHandler;
    private readonly IQuestionRepository _questionRepository;

    public QuestionsController(
        ILogger<QuestionsController> logger,
        CreateQuestionCommandHandler createHandler,
        UpdateQuestionCommandHandler updateHandler,
        DeleteQuestionCommandHandler deleteHandler,
        GetQuestionsQueryHandler getQuestionsHandler,
        GetQuestionByIdQueryHandler getQuestionByIdHandler,
        IQuestionRepository questionRepository)
    {
        _logger = logger;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getQuestionsHandler = getQuestionsHandler;
        _getQuestionByIdHandler = getQuestionByIdHandler;
        _questionRepository = questionRepository;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get paginated list of questions with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<QuestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<QuestionDto>>> GetQuestions(
        [FromQuery] GetQuestionsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting questions with params: {@Query}", query);
        
        var result = await _getQuestionsHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get question details by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(QuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionDto>> GetQuestion(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting question with ID: {QuestionId}", id);
        
        var query = new GetQuestionByIdQuery { QuestionId = id };
        var result = await _getQuestionByIdHandler.HandleAsync(query, cancellationToken);
        
        if (result == null)
            return NotFound(new { message = $"Question with ID {id} not found" });
        
        // Increment view count asynchronously (fire and forget)
        _ = _questionRepository.IncrementViewCountAsync(id, CancellationToken.None);
            
        return Ok(result);
    }

    /// <summary>
    /// Create a new question
    /// </summary>
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
        
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        
        return CreatedAtAction(
            nameof(GetQuestion), 
            new { id = result.QuestionId }, 
            result);
    }

    /// <summary>
    /// Update an existing question
    /// </summary>
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
        
        var success = await _updateHandler.HandleAsync(command, cancellationToken);
        
        if (!success)
            return NotFound();
            
        return Ok(new { message = "Question updated successfully" });
    }

    /// <summary>
    /// Delete a question
    /// </summary>
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
        
        var command = new DeleteQuestionCommand 
        { 
            QuestionId = id,
            UserId = userId
        };
        
        var success = await _deleteHandler.HandleAsync(command, cancellationToken);
        
        if (!success)
            return NotFound();
            
        return NoContent();
    }
}
