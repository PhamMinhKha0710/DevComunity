using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Queries.Users;
using DevComunity.Application.QueryHandlers.Users;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for User profiles
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly GetUserByIdQueryHandler _getUserByIdHandler;

    public UsersController(
        ILogger<UsersController> logger,
        GetUserByIdQueryHandler getUserByIdHandler)
    {
        _logger = logger;
        _getUserByIdHandler = getUserByIdHandler;
    }

    /// <summary>
    /// Get user profile by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user profile {UserId}", id);

        var query = new GetUserByIdQuery { UserId = id };
        var result = await _getUserByIdHandler.HandleAsync(query, cancellationToken);

        if (result == null)
            return NotFound(new { message = $"User with ID {id} not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get user's questions
    /// </summary>
    [HttpGet("{id:int}/questions")]
    [ProducesResponseType(typeof(PaginatedResponse<QuestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<QuestionDto>>> GetUserQuestions(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting questions for user {UserId}", id);

        // TODO: Implement GetUserQuestionsQueryHandler
        return Ok(new PaginatedResponse<QuestionDto>
        {
            Items = new List<QuestionDto>(),
            Page = page,
            PageSize = pageSize,
            TotalCount = 0
        });
    }

    /// <summary>
    /// Get user's answers
    /// </summary>
    [HttpGet("{id:int}/answers")]
    [ProducesResponseType(typeof(PaginatedResponse<AnswerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<AnswerDto>>> GetUserAnswers(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting answers for user {UserId}", id);

        // TODO: Implement GetUserAnswersQueryHandler
        return Ok(new PaginatedResponse<AnswerDto>
        {
            Items = new List<AnswerDto>(),
            Page = page,
            PageSize = pageSize,
            TotalCount = 0
        });
    }

    /// <summary>
    /// Get user's badges
    /// </summary>
    [HttpGet("{id:int}/badges")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserBadges(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting badges for user {UserId}", id);

        // TODO: Implement GetUserBadgesQueryHandler
        return Ok(new List<object>());
    }
}
