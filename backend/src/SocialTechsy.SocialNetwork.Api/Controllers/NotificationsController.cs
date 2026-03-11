using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Commands.Notifications;
using SocialTechsy.SocialNetwork.Application.Queries.Notifications;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;
    private readonly IMediator _mediator;

    public NotificationsController(ILogger<NotificationsController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("Getting notifications for user {UserId}", userId);

        var result = await _mediator.Send(new GetNotificationsQuery
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize,
            UnreadOnly = unreadOnly
        }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var count = await _mediator.Send(new GetUnreadCountQuery { UserId = userId }, cancellationToken);
        return Ok(new { count });
    }

    [HttpPut("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("Marking notification {NotificationId} as read for user {UserId}", id, userId);

        var success = await _mediator.Send(new MarkNotificationReadCommand { NotificationId = id, UserId = userId }, cancellationToken);
        if (!success)
            return NotFound(new { message = "Notification not found" });

        return Ok(new { message = "Notification marked as read" });
    }

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("Marking all notifications as read for user {UserId}", userId);

        await _mediator.Send(new MarkAllNotificationsReadCommand { UserId = userId }, cancellationToken);

        return Ok(new { message = "All notifications marked as read" });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("Deleting notification {NotificationId} for user {UserId}", id, userId);

        var success = await _mediator.Send(new DeleteNotificationCommand { NotificationId = id, UserId = userId }, cancellationToken);
        if (!success)
            return NotFound(new { message = "Notification not found" });

        return NoContent();
    }
}
