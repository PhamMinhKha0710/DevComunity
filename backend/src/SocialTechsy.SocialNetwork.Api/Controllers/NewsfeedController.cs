using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Api.Hubs;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

/// <summary>
/// API Controller for Newsfeed and Posts
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NewsfeedController : ControllerBase
{
    private readonly ILogger<NewsfeedController> _logger;
    private readonly IPostRepository _postRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IHubContext<ActivityHub> _activityHub;
    private readonly ILikeService _likeService;
    private readonly ICommentRepository _commentRepository;
    private readonly ISavedItemRepository _savedItemRepository;

    public NewsfeedController(
        ILogger<NewsfeedController> logger,
        IPostRepository postRepository,
        IGroupRepository groupRepository,
        IHubContext<ActivityHub> activityHub,
        ILikeService likeService,
        ICommentRepository commentRepository,
        ISavedItemRepository savedItemRepository)
    {
        _logger = logger;
        _postRepository = postRepository;
        _groupRepository = groupRepository;
        _activityHub = activityHub;
        _likeService = likeService;
        _commentRepository = commentRepository;
        _savedItemRepository = savedItemRepository;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get personalized newsfeed (posts from friends, followed users, and groups)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<PostDto>>> GetNewsfeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var (items, totalCount) = await _postRepository.GetNewsfeedAsync(userId, page, pageSize, cancellationToken);
        var dtos = await EnrichWithEngagementAsync(items, userId, cancellationToken);

        return Ok(new PaginatedResponse<PostDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get posts from a specific group
    /// </summary>
    [HttpGet("groups/{groupId:int}")]
    [ProducesResponseType(typeof(PaginatedResponse<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<PostDto>>> GetGroupPosts(
        int groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        // Check if user is a member of the group
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group == null) return NotFound(new { message = "Group not found" });

        if (group.IsPrivate)
        {
            var isMember = await _groupRepository.IsMemberAsync(groupId, userId, cancellationToken);
            if (!isMember) return Forbid();
        }

        var (items, totalCount) = await _postRepository.GetGroupPostsAsync(groupId, page, pageSize, cancellationToken);
        var dtos = await EnrichWithEngagementAsync(items, userId, cancellationToken);

        return Ok(new PaginatedResponse<PostDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get posts by a specific user
    /// </summary>
    [HttpGet("users/{targetUserId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<PostDto>>> GetUserPosts(
        int targetUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _postRepository.GetUserPostsAsync(targetUserId, page, pageSize, cancellationToken);

        // Filter to only public posts for non-friends
        var publicPosts = items.Where(p => p.GroupId == null).ToList();
        var currentUserId = GetCurrentUserId();
        var dtos = await EnrichWithEngagementAsync(publicPosts, currentUserId, cancellationToken);

        return Ok(new PaginatedResponse<PostDto>
        {
            Items = dtos,
            TotalCount = publicPosts.Count,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Create a new post
    /// </summary>
    [HttpPost("posts")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PostDto>> CreatePost(
        [FromBody] CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        // If posting to a group, verify membership
        if (request.GroupId.HasValue)
        {
            var group = await _groupRepository.GetByIdAsync(request.GroupId.Value, cancellationToken);
            if (group == null) return NotFound(new { message = "Group not found" });

            var isMember = await _groupRepository.IsMemberAsync(request.GroupId.Value, userId, cancellationToken);
            if (!isMember) return Forbid();
        }

        var post = new Post
        {
            AuthorId = userId,
            GroupId = request.GroupId,
            Content = request.Content
        };

        var created = await _postRepository.AddAsync(post, cancellationToken);
        
        // Reload with navigation properties
        var result = await _postRepository.GetByIdAsync(created.PostId, cancellationToken);

        _logger.LogInformation("User {UserId} created post {PostId}", userId, created.PostId);

        // Real-time broadcast to activity feed (or group members)
        try
        {
            var postDto = MapToDto(result!);
            if (request.GroupId.HasValue)
            {
                // Broadcast to group members
                await _activityHub.Clients.Group($"group_{request.GroupId}")
                    .SendAsync("NewGroupPost", postDto, cancellationToken);
            }
            else
            {
                // Broadcast to global activity feed (friends will filter)
                await _activityHub.Clients.Group("activity_feed")
                    .SendAsync("NewPost", postDto, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting new post");
        }

        return Created($"/api/newsfeed/posts/{result!.PostId}", MapToDto(result));
    }

    /// <summary>
    /// Get a specific post
    /// </summary>
    [HttpGet("posts/{postId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> GetPost(int postId, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post == null) return NotFound(new { message = "Post not found" });

        // Check access for group posts
        if (post.GroupId.HasValue && post.Group?.IsPrivate == true)
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                var isMember = await _groupRepository.IsMemberAsync(post.GroupId.Value, userId, cancellationToken);
                if (!isMember) return Forbid();
            }
            else
            {
                return Forbid();
            }
        }

        var currentUserId = GetCurrentUserId();
        var dtos = await EnrichWithEngagementAsync(new[] { post }, currentUserId, cancellationToken);
        return Ok(dtos[0]);
    }

    /// <summary>
    /// Update a post
    /// </summary>
    [HttpPut("posts/{postId:int}")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PostDto>> UpdatePost(
        int postId,
        [FromBody] UpdatePostRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post == null) return NotFound(new { message = "Post not found" });

        // Only author can edit
        if (post.AuthorId != userId) return Forbid();

        post.Content = request.Content;
        await _postRepository.UpdateAsync(post, cancellationToken);

        _logger.LogInformation("User {UserId} updated post {PostId}", userId, postId);

        var dtos = await EnrichWithEngagementAsync(new[] { post }, userId, cancellationToken);
        return Ok(dtos[0]);
    }

    /// <summary>
    /// Like a post
    /// </summary>
    [HttpPost("posts/{postId:int}/like")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LikePost(int postId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post == null) return NotFound(new { message = "Post not found" });

        var count = await _likeService.LikeAsync("post", postId, userId);
        _logger.LogInformation("User {UserId} liked post {PostId}, likeCount={Count}", userId, postId, count);
        return Ok(new { likeCount = count, userLiked = true });
    }

    /// <summary>
    /// Remove like from a post
    /// </summary>
    [HttpDelete("posts/{postId:int}/like")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlikePost(int postId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post == null) return NotFound(new { message = "Post not found" });

        var count = await _likeService.UnlikeAsync("post", postId, userId);
        _logger.LogInformation("User {UserId} unliked post {PostId}, likeCount={Count}", userId, postId, count);
        return Ok(new { likeCount = count, userLiked = false });
    }

    /// <summary>
    /// Delete a post
    /// </summary>
    [HttpDelete("posts/{postId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePost(int postId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post == null) return NotFound(new { message = "Post not found" });

        // Author or group admin can delete
        if (post.AuthorId != userId)
        {
            if (post.GroupId.HasValue)
            {
                var member = await _groupRepository.GetMemberAsync(post.GroupId.Value, userId, cancellationToken);
                if (member == null || (member.Role != GroupRole.Admin && member.Role != GroupRole.Moderator))
                    return Forbid();
            }
            else
            {
                return Forbid();
            }
        }

        await _postRepository.DeleteAsync(postId, cancellationToken);

        _logger.LogInformation("User {UserId} deleted post {PostId}", userId, postId);

        return Ok(new { message = "Post deleted" });
    }

    private async Task<List<PostDto>> EnrichWithEngagementAsync(IEnumerable<Post> posts, int userId, CancellationToken cancellationToken)
    {
        var list = posts.ToList();
        if (list.Count == 0) return new List<PostDto>();

        var postIds = list.Select(p => p.PostId).ToArray();
        var likeCounts = await _likeService.GetLikeCountsBatchAsync("post", postIds);

        // Get comment counts for all posts
        var commentCounts = new Dictionary<int, int>();
        foreach (var pid in postIds)
        {
            commentCounts[pid] = await _commentRepository.GetCountByPostIdAsync(pid, cancellationToken);
        }

        var result = new List<PostDto>(list.Count);
        foreach (var p in list)
        {
            var dto = MapToDto(p);
            dto.LikeCount = (int)(likeCounts.GetValueOrDefault(p.PostId, 0));
            dto.UserLiked = userId > 0 && await _likeService.IsLikedAsync("post", p.PostId, userId);
            dto.CommentCount = commentCounts.GetValueOrDefault(p.PostId, 0);
            dto.UserSaved = userId > 0 && await _savedItemRepository.IsSavedAsync(userId, null, null, p.PostId, cancellationToken);
            result.Add(dto);
        }
        return result;
    }

    private static PostDto MapToDto(Post p) => new()
    {
        PostId = p.PostId,
        Author = new UserSummaryDto
        {
            UserId = p.Author.UserId,
            Username = p.Author.Username,
            DisplayName = p.Author.DisplayName,
            ProfilePicture = p.Author.ProfilePicture
        },
        GroupId = p.GroupId,
        GroupName = p.Group?.Name,
        Content = p.Content,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
