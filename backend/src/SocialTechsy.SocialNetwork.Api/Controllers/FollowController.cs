using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Api.Hubs;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

/// <summary>
/// API Controller for Follow/Unfollow functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FollowController : ControllerBase
{
    private readonly ILogger<FollowController> _logger;
    private readonly IFollowRepository _followRepository;
    private readonly IUserRepository _userRepository;

    public FollowController(
        ILogger<FollowController> logger,
        IFollowRepository followRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IHubContext<NotificationHub> hubContext)
    {
        _logger = logger;
        _followRepository = followRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _hubContext = hubContext;
    }

    private readonly INotificationRepository _notificationRepository;
    private readonly IHubContext<NotificationHub> _hubContext;

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get followers of a user
    /// </summary>
    [HttpGet("followers/{userId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<FollowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FollowDto>>> GetFollowers(int userId, CancellationToken cancellationToken)
    {
        var followers = await _followRepository.GetFollowersAsync(userId, cancellationToken);
        
        var result = followers.Select(f => new FollowDto
        {
            UserId = f.Follower.UserId,
            Username = f.Follower.Username,
            DisplayName = f.Follower.DisplayName,
            ProfilePicture = f.Follower.ProfilePicture,
            FollowedAt = f.CreatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Get users that a user is following
    /// </summary>
    [HttpGet("following/{userId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<FollowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FollowDto>>> GetFollowing(int userId, CancellationToken cancellationToken)
    {
        var following = await _followRepository.GetFollowingAsync(userId, cancellationToken);
        
        var result = following.Select(f => new FollowDto
        {
            UserId = f.Following.UserId,
            Username = f.Following.Username,
            DisplayName = f.Following.DisplayName,
            ProfilePicture = f.Following.ProfilePicture,
            FollowedAt = f.CreatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Get follow statistics for a user
    /// </summary>
    [HttpGet("stats/{userId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FollowStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FollowStatsDto>> GetFollowStats(int userId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        
        var followersCount = await _followRepository.GetFollowersCountAsync(userId, cancellationToken);
        var followingCount = await _followRepository.GetFollowingCountAsync(userId, cancellationToken);
        var isFollowing = currentUserId > 0 && 
            await _followRepository.IsFollowingAsync(currentUserId, userId, cancellationToken);

        return Ok(new FollowStatsDto
        {
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            IsFollowing = isFollowing
        });
    }

    /// <summary>
    /// Follow a user
    /// </summary>
    [HttpPost("{targetUserId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Follow(int targetUserId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();
        if (userId == targetUserId) return BadRequest(new { message = "Cannot follow yourself" });

        // Check if target user exists
        var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (targetUser == null) return NotFound(new { message = "User not found" });

        // Check if already following
        var isFollowing = await _followRepository.IsFollowingAsync(userId, targetUserId, cancellationToken);
        if (isFollowing) return BadRequest(new { message = "Already following this user" });

        var follow = new UserFollow
        {
            FollowerId = userId,
            FollowingId = targetUserId
        };

        await _followRepository.FollowAsync(follow, cancellationToken);

        _logger.LogInformation("User {UserId} followed user {TargetId}", userId, targetUserId);
        
        // Create and send notification
        try 
        {
            var follower = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (follower != null)
            {
                var notification = new Notification
                {
                    UserId = targetUserId,
                    FromUserId = userId,
                    Type = "Follow",
                    Message = $"{follower.DisplayName ?? follower.Username} started following you",
                    Link = $"/users/{userId}",
                    CreatedDate = DateTime.UtcNow,
                    IsRead = false
                };

                await _notificationRepository.AddAsync(notification, cancellationToken);
                
                await _hubContext.Clients.Group($"user_{targetUserId}").SendAsync("ReceiveNotification", notification, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending follow notification");
            // Don't fail the request if notification fails
        }
        
        return Ok(new { message = "Successfully followed user" });
    }

    /// <summary>
    /// Unfollow a user
    /// </summary>
    [HttpDelete("{targetUserId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Unfollow(int targetUserId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        await _followRepository.UnfollowAsync(userId, targetUserId, cancellationToken);

        _logger.LogInformation("User {UserId} unfollowed user {TargetId}", userId, targetUserId);
        
        return Ok(new { message = "Successfully unfollowed user" });
    }

    /// <summary>
    /// Check if current user is following a target user
    /// </summary>
    [HttpGet("check/{targetUserId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> CheckFollowing(int targetUserId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var isFollowing = await _followRepository.IsFollowingAsync(userId, targetUserId, cancellationToken);
        
        return Ok(new { isFollowing });
    }
}
