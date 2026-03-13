using MediatR;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Badges;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BadgesController : ControllerBase
{
    private readonly ILogger<BadgesController> _logger;
    private readonly IMediator _mediator;

    public BadgesController(ILogger<BadgesController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BadgeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BadgeDto>>> GetBadges(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all badges");
        var result = await _mediator.Send(new GetBadgesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BadgeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BadgeDto>> GetBadge(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting badge {BadgeId}", id);

        var result = await _mediator.Send(new GetBadgeByIdQuery { BadgeId = id }, cancellationToken);

        if (result == null)
            return NotFound(new { message = $"Badge with ID {id} not found" });

        return Ok(result);
    }

    [HttpGet("{id:int}/users")]
    [ProducesResponseType(typeof(PaginatedResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetBadgeUsers(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting users for badge {BadgeId}", id);

        var result = await _mediator.Send(new GetBadgeUsersQuery { BadgeId = id, Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(result);
    }
}
