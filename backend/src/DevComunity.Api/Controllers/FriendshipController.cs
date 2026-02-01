using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Api.Hubs;
using System.Security.Claims;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Friendship/Friend Requests
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendshipController : ControllerBase
{
    private readonly ILogger<FriendshipController> _logger;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHubContext<NotificationHub> _hubContext;

    public FriendshipController(
        ILogger<FriendshipController> logger,
        IFriendshipRepository friendshipRepository,
        IUserRepository userRepository,
        IHubContext<NotificationHub> hubContext)
    {
        _logger = logger;
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
        _hubContext = hubContext;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get current user's friends list
    /// </summary>
    [HttpGet("friends")]
    [ProducesResponseType(typeof(IEnumerable<FriendDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FriendDto>>> GetFriends(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var friendships = await _friendshipRepository.GetFriendsAsync(userId, cancellationToken);
        
        var friends = friendships.Select(f =>
        {
            var friend = f.RequesterId == userId ? f.Addressee : f.Requester;
            return new FriendDto
            {
                UserId = friend.UserId,
                Username = friend.Username,
                DisplayName = friend.DisplayName,
                ProfilePicture = friend.ProfilePicture,
                FriendsSince = f.RespondedAt ?? f.CreatedAt
            };
        });

        return Ok(friends);
    }

    /// <summary>
    /// Get pending friend requests (received by current user)
    /// </summary>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(IEnumerable<FriendshipDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FriendshipDto>>> GetPendingRequests(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var requests = await _friendshipRepository.GetPendingRequestsAsync(userId, cancellationToken);
        
        return Ok(requests.Select(MapToDto));
    }

    /// <summary>
    /// Get sent friend requests (by current user)
    /// </summary>
    [HttpGet("requests/sent")]
    [ProducesResponseType(typeof(IEnumerable<FriendshipDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FriendshipDto>>> GetSentRequests(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var requests = await _friendshipRepository.GetSentRequestsAsync(userId, cancellationToken);
        
        return Ok(requests.Select(MapToDto));
    }

    /// <summary>
    /// Send a friend request to another user
    /// </summary>
    [HttpPost("request/{targetUserId:int}")]
    [ProducesResponseType(typeof(FriendshipDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FriendshipDto>> SendFriendRequest(int targetUserId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();
        if (userId == targetUserId) return BadRequest(new { message = "Cannot send friend request to yourself" });

        // Check if target user exists
        var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (targetUser == null) return NotFound(new { message = "User not found" });

        // Check if friendship already exists
        var existing = await _friendshipRepository.GetFriendshipAsync(userId, targetUserId, cancellationToken);
        if (existing != null)
        {
            if (existing.Status == FriendshipStatus.Accepted)
                return BadRequest(new { message = "Already friends" });
            if (existing.Status == FriendshipStatus.Pending)
                return BadRequest(new { message = "Friend request already pending" });
        }

        var friendship = new Friendship
        {
            RequesterId = userId,
            AddresseeId = targetUserId
        };

        var created = await _friendshipRepository.AddAsync(friendship, cancellationToken);
        
        // Reload with navigation properties
        var result = await _friendshipRepository.GetByIdAsync(created.FriendshipId, cancellationToken);
        
        _logger.LogInformation("User {UserId} sent friend request to {TargetId}", userId, targetUserId);

        // Real-time notification to target user
        try
        {
            await _hubContext.Clients.Group($"user_{targetUserId}")
                .SendAsync("FriendRequestReceived", new
                {
                    friendshipId = result!.FriendshipId,
                    fromUser = new { 
                        userId = result.Requester.UserId,
                        username = result.Requester.Username,
                        displayName = result.Requester.DisplayName,
                        profilePicture = result.Requester.ProfilePicture
                    },
                    createdAt = result.CreatedAt
                }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time friend request notification");
        }
        
        return Created($"/api/friendship/{result!.FriendshipId}", MapToDto(result));
    }

    /// <summary>
    /// Accept a friend request
    /// </summary>
    [HttpPut("accept/{friendshipId:int}")]
    [ProducesResponseType(typeof(FriendshipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FriendshipDto>> AcceptFriendRequest(int friendshipId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId, cancellationToken);
        if (friendship == null) return NotFound(new { message = "Friend request not found" });
        if (friendship.AddresseeId != userId) return Forbid();
        if (friendship.Status != FriendshipStatus.Pending) 
            return BadRequest(new { message = "Request is no longer pending" });

        friendship.Status = FriendshipStatus.Accepted;
        friendship.RespondedAt = DateTime.UtcNow;
        await _friendshipRepository.UpdateAsync(friendship, cancellationToken);

        _logger.LogInformation("User {UserId} accepted friend request from {RequesterId}", userId, friendship.RequesterId);

        // Real-time notification to requester
        try
        {
            await _hubContext.Clients.Group($"user_{friendship.RequesterId}")
                .SendAsync("FriendRequestAccepted", new
                {
                    friendshipId = friendship.FriendshipId,
                    acceptedBy = new {
                        userId = friendship.Addressee.UserId,
                        username = friendship.Addressee.Username,
                        displayName = friendship.Addressee.DisplayName,
                        profilePicture = friendship.Addressee.ProfilePicture
                    },
                    acceptedAt = friendship.RespondedAt
                }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time friend accepted notification");
        }
        
        return Ok(MapToDto(friendship));
    }

    /// <summary>
    /// Reject a friend request
    /// </summary>
    [HttpPut("reject/{friendshipId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectFriendRequest(int friendshipId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId, cancellationToken);
        if (friendship == null) return NotFound(new { message = "Friend request not found" });
        if (friendship.AddresseeId != userId) return Forbid();

        friendship.Status = FriendshipStatus.Rejected;
        friendship.RespondedAt = DateTime.UtcNow;
        await _friendshipRepository.UpdateAsync(friendship, cancellationToken);

        _logger.LogInformation("User {UserId} rejected friend request from {RequesterId}", userId, friendship.RequesterId);

        // Real-time notification to requester (optional, can be silent)
        try
        {
            await _hubContext.Clients.Group($"user_{friendship.RequesterId}")
                .SendAsync("FriendRequestRejected", new
                {
                    friendshipId = friendship.FriendshipId,
                    rejectedBy = userId
                }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time friend rejected notification");
        }
        
        return Ok(new { message = "Friend request rejected" });
    }

    /// <summary>
    /// Cancel a sent friend request or unfriend
    /// </summary>
    [HttpDelete("{friendshipId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteFriendship(int friendshipId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId, cancellationToken);
        if (friendship == null) return NotFound(new { message = "Friendship not found" });
        if (friendship.RequesterId != userId && friendship.AddresseeId != userId) return Forbid();

        await _friendshipRepository.DeleteAsync(friendshipId, cancellationToken);

        _logger.LogInformation("User {UserId} deleted friendship {FriendshipId}", userId, friendshipId);
        
        return Ok(new { message = "Friendship removed" });
    }

    /// <summary>
    /// Check if two users are friends
    /// </summary>
    [HttpGet("check/{targetUserId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> CheckFriendship(int targetUserId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var friendship = await _friendshipRepository.GetFriendshipAsync(userId, targetUserId, cancellationToken);
        
        return Ok(new
        {
            areFriends = friendship?.Status == FriendshipStatus.Accepted,
            requestPending = friendship?.Status == FriendshipStatus.Pending,
            friendshipId = friendship?.FriendshipId,
            isSentByMe = friendship?.RequesterId == userId
        });
    }

    private static FriendshipDto MapToDto(Friendship f) => new()
    {
        FriendshipId = f.FriendshipId,
        Requester = new UserSummaryDto
        {
            UserId = f.Requester.UserId,
            Username = f.Requester.Username,
            DisplayName = f.Requester.DisplayName,
            ProfilePicture = f.Requester.ProfilePicture
        },
        Addressee = new UserSummaryDto
        {
            UserId = f.Addressee.UserId,
            Username = f.Addressee.Username,
            DisplayName = f.Addressee.DisplayName,
            ProfilePicture = f.Addressee.ProfilePicture
        },
        Status = f.Status.ToString(),
        CreatedAt = f.CreatedAt,
        RespondedAt = f.RespondedAt
    };
}
