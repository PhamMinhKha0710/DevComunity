using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Queries.Users;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Users;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Badges;
using SocialTechsy.SocialNetwork.Application.Commands.Users;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IMediator _mediator;

    public UsersController(ILogger<UsersController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileCommand command,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        _logger.LogInformation("Updating profile for user {UserId}", userId);

        command.UserId = userId;
        var success = await _mediator.Send(command, cancellationToken);

        if (!success)
            return NotFound(new { message = "User not found" });

        return Ok(new { message = "Profile updated successfully" });
    }

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

        var result = await _mediator.Send(new GetUsersQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            SortBy = sortBy
        }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user profile {UserId}", id);

        var result = await _mediator.Send(new GetUserByIdQuery { UserId = id }, cancellationToken);

        if (result == null)
            return NotFound(new { message = $"User with ID {id} not found" });

        return Ok(result);
    }

    [HttpGet("{id:int}/questions")]
    [ProducesResponseType(typeof(PaginatedResponse<QuestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<QuestionDto>>> GetUserQuestions(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting questions for user {UserId}", id);

        var result = await _mediator.Send(new GetUserQuestionsQuery { UserId = id, Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/answers")]
    [ProducesResponseType(typeof(PaginatedResponse<AnswerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<AnswerDto>>> GetUserAnswers(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting answers for user {UserId}", id);

        var result = await _mediator.Send(new GetUserAnswersQuery { UserId = id, Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/badges")]
    [ProducesResponseType(typeof(IEnumerable<UserBadgeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserBadgeDto>>> GetUserBadges(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting badges for user {UserId}", id);

        var result = await _mediator.Send(new GetUserBadgesQuery { UserId = id }, cancellationToken);
        return Ok(result);
    }
}
