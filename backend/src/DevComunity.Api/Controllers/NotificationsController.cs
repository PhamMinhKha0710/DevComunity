using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Commands.Notifications;
using DevComunity.Application.CommandHandlers.Notifications;
using DevComunity.Application.Queries.Notifications;
using DevComunity.Application.QueryHandlers.Notifications;
using System.Security.Claims;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for User Notifications
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;
    private readonly GetNotificationsQueryHandler _getNotificationsHandler;
    private readonly GetUnreadCountQueryHandler _getUnreadCountHandler;
    private readonly MarkNotificationReadCommandHandler _markReadHandler;
    private readonly MarkAllNotificationsReadCommandHandler _markAllReadHandler;
    private readonly DeleteNotificationCommandHandler _deleteHandler;

    public NotificationsController(
        ILogger<NotificationsController> logger,
        GetNotificationsQueryHandler getNotificationsHandler,
        GetUnreadCountQueryHandler getUnreadCountHandler,
        MarkNotificationReadCommandHandler markReadHandler,
        MarkAllNotificationsReadCommandHandler markAllReadHandler,
        DeleteNotificationCommandHandler deleteHandler)
    {
        _logger = logger;
        _getNotificationsHandler = getNotificationsHandler;
        _getUnreadCountHandler = getUnreadCountHandler;
        _markReadHandler = markReadHandler;
        _markAllReadHandler = markAllReadHandler;
        _deleteHandler = deleteHandler;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get user's notifications
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("Getting notifications for user {UserId}", userId);

        var query = new GetNotificationsQuery
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize,
            UnreadOnly = unreadOnly
        };

        var result = await _getNotificationsHandler.HandleAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        var query = new GetUnreadCountQuery { UserId = userId };
        var count = await _getUnreadCountHandler.HandleAsync(query, cancellationToken);
        return Ok(new { count });
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPut("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("Marking notification {NotificationId} as read for user {UserId}", id, userId);

        var command = new MarkNotificationReadCommand
        {
            NotificationId = id,
            UserId = userId
        };

        var success = await _markReadHandler.HandleAsync(command, cancellationToken);
        if (!success)
            return NotFound(new { message = "Notification not found" });

        return Ok(new { message = "Notification marked as read" });
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("Marking all notifications as read for user {UserId}", userId);

        var command = new MarkAllNotificationsReadCommand { UserId = userId };
        await _markAllReadHandler.HandleAsync(command, cancellationToken);

        return Ok(new { message = "All notifications marked as read" });
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        _logger.LogInformation("Deleting notification {NotificationId} for user {UserId}", id, userId);

        var command = new DeleteNotificationCommand
        {
            NotificationId = id,
            UserId = userId
        };

        var success = await _deleteHandler.HandleAsync(command, cancellationToken);
        if (!success)
            return NotFound(new { message = "Notification not found" });

        return NoContent();
    }
}
