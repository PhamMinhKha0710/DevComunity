using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Queries.Users;
using DevComunity.Application.QueryHandlers.Users;
using DevComunity.Application.QueryHandlers.Badges;

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
    private readonly GetUsersQueryHandler _getUsersHandler;
    private readonly GetUserQuestionsQueryHandler _getUserQuestionsHandler;
    private readonly GetUserAnswersQueryHandler _getUserAnswersHandler;
    private readonly GetUserBadgesQueryHandler _getUserBadgesHandler;

    public UsersController(
        ILogger<UsersController> logger,
        GetUserByIdQueryHandler getUserByIdHandler,
        GetUsersQueryHandler getUsersHandler,
        GetUserQuestionsQueryHandler getUserQuestionsHandler,
        GetUserAnswersQueryHandler getUserAnswersHandler,
        GetUserBadgesQueryHandler getUserBadgesHandler)
    {
        _logger = logger;
        _getUserByIdHandler = getUserByIdHandler;
        _getUsersHandler = getUsersHandler;
        _getUserQuestionsHandler = getUserQuestionsHandler;
        _getUserAnswersHandler = getUserAnswersHandler;
        _getUserBadgesHandler = getUserBadgesHandler;
    }

    /// <summary>
    /// Get paginated list of users
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 36,
        [FromQuery] string sortBy = "reputation",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting users with search: {Search}, sortBy: {SortBy}", search, sortBy);

        var query = new GetUsersQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            SortBy = sortBy
        };

        var result = await _getUsersHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
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

        var result = await _getUserQuestionsHandler.HandleAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
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

        var result = await _getUserAnswersHandler.HandleAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get user's badges
    /// </summary>
    [HttpGet("{id:int}/badges")]
    [ProducesResponseType(typeof(IEnumerable<UserBadgeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserBadgeDto>>> GetUserBadges(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting badges for user {UserId}", id);

        var result = await _getUserBadgesHandler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }
}
