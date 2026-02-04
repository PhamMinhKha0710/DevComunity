using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Badges;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

/// <summary>
/// API Controller for Badges
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BadgesController : ControllerBase
{
    private readonly ILogger<BadgesController> _logger;
    private readonly GetBadgesQueryHandler _getBadgesHandler;
    private readonly GetBadgeByIdQueryHandler _getBadgeByIdHandler;
    private readonly GetBadgeUsersQueryHandler _getBadgeUsersHandler;

    public BadgesController(
        ILogger<BadgesController> logger,
        GetBadgesQueryHandler getBadgesHandler,
        GetBadgeByIdQueryHandler getBadgeByIdHandler,
        GetBadgeUsersQueryHandler getBadgeUsersHandler)
    {
        _logger = logger;
        _getBadgesHandler = getBadgesHandler;
        _getBadgeByIdHandler = getBadgeByIdHandler;
        _getBadgeUsersHandler = getBadgeUsersHandler;
    }

    /// <summary>
    /// Get all available badges
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BadgeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BadgeDto>>> GetBadges(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all badges");

        var result = await _getBadgesHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get badge by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BadgeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BadgeDto>> GetBadge(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting badge {BadgeId}", id);

        var result = await _getBadgeByIdHandler.HandleAsync(id, cancellationToken);
        
        if (result == null)
            return NotFound(new { message = $"Badge with ID {id} not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get users who earned a badge
    /// </summary>
    [HttpGet("{id:int}/users")]
    [ProducesResponseType(typeof(PaginatedResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetBadgeUsers(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting users for badge {BadgeId}", id);

        var result = await _getBadgeUsersHandler.HandleAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
