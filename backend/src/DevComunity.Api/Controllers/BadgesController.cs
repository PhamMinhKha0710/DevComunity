using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Badges
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BadgesController : ControllerBase
{
    private readonly ILogger<BadgesController> _logger;

    public BadgesController(ILogger<BadgesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all available badges
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBadges(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all badges");

        // TODO: Implement GetBadgesQueryHandler
        return Ok(new List<object>());
    }

    /// <summary>
    /// Get badge by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBadge(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting badge {BadgeId}", id);

        // TODO: Implement GetBadgeByIdQueryHandler
        return NotFound(new { message = $"Badge with ID {id} not found" });
    }

    /// <summary>
    /// Get users who earned a badge
    /// </summary>
    [HttpGet("{id:int}/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBadgeUsers(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting users for badge {BadgeId}", id);

        // TODO: Implement GetBadgeUsersQueryHandler
        return Ok(new { items = new List<object>(), page, pageSize, totalCount = 0 });
    }
}
