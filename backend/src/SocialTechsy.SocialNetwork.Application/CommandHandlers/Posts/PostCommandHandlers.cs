using MediatR;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Commands.Posts;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Posts;

public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, PostDto>
{
    private readonly IPostRepository _postRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActivityEventDispatcher _activityDispatcher;
    private readonly ILogger<CreatePostCommandHandler> _logger;

    public CreatePostCommandHandler(
        IPostRepository postRepository,
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork,
        IActivityEventDispatcher activityDispatcher,
        ILogger<CreatePostCommandHandler> logger)
    {
        _postRepository = postRepository;
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
        _activityDispatcher = activityDispatcher;
        _logger = logger;
    }

    public async Task<PostDto> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        if (request.GroupId.HasValue)
        {
            var group = await _groupRepository.GetByIdAsync(request.GroupId.Value, cancellationToken);
            if (group == null)
                throw new InvalidOperationException("Group not found");

            var isMember = await _groupRepository.IsMemberAsync(request.GroupId.Value, request.AuthorId, cancellationToken);
            if (!isMember)
                throw new UnauthorizedAccessException("User is not a member of this group");
        }

        var post = new Post
        {
            AuthorId = request.AuthorId,
            GroupId = request.GroupId,
            Content = request.Content,
            MediaUrls = request.MediaUrls,
            Visibility = request.Visibility,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _postRepository.AddAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} created post {PostId}", request.AuthorId, created.PostId);

        var result = await _postRepository.GetByIdAsync(created.PostId, cancellationToken);

        var dto = result != null
            ? MapToDto(result)
            : throw new InvalidOperationException($"Post {created.PostId} not found after creation");

        try
        {
            if (request.GroupId.HasValue)
            {
                await _activityDispatcher.BroadcastNewGroupPostAsync(request.GroupId.Value, dto, cancellationToken);
            }
            else
            {
                await _activityDispatcher.BroadcastNewPostAsync(dto, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting new post");
        }

        return dto;
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
        MediaUrls = p.MediaUrls,
        Visibility = p.Visibility,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}

public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, PostDto>
{
    private readonly IPostRepository _postRepository;
    private readonly ILikeService _likeService;
    private readonly ICommentRepository _commentRepository;
    private readonly ISavedItemRepository _savedItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePostCommandHandler> _logger;

    public UpdatePostCommandHandler(
        IPostRepository postRepository,
        ILikeService likeService,
        ICommentRepository commentRepository,
        ISavedItemRepository savedItemRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdatePostCommandHandler> logger)
    {
        _postRepository = postRepository;
        _likeService = likeService;
        _commentRepository = commentRepository;
        _savedItemRepository = savedItemRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PostDto> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null)
            throw new InvalidOperationException("Post not found");

        if (post.AuthorId != request.UserId)
            throw new UnauthorizedAccessException("Only the author can edit this post");

        post.Content = request.Content;
        post.MediaUrls = request.MediaUrls;
        await _postRepository.UpdateAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} updated post {PostId}", request.UserId, request.PostId);

        return await EnrichAsync(post, request.UserId, cancellationToken);
    }

    private async Task<PostDto> EnrichAsync(Post p, int userId, CancellationToken cancellationToken)
    {
        var likeCount = await _likeService.GetLikeCountAsync("post", p.PostId);
        var userLiked = userId > 0 && await _likeService.IsLikedAsync("post", p.PostId, userId);
        var commentCount = await _commentRepository.GetCountByPostIdAsync(p.PostId, cancellationToken);
        var userSaved = userId > 0 && await _savedItemRepository.IsSavedAsync(userId, null, null, p.PostId, cancellationToken);

        return new PostDto
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
            MediaUrls = p.MediaUrls,
            Visibility = p.Visibility,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            LikeCount = (int)likeCount,
            UserLiked = userLiked,
            CommentCount = commentCount,
            UserSaved = userSaved
        };
    }
}

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, bool>
{
    private readonly IPostRepository _postRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ILogger<DeletePostCommandHandler> _logger;

    public DeletePostCommandHandler(
        IPostRepository postRepository,
        IGroupRepository groupRepository,
        ILogger<DeletePostCommandHandler> logger)
    {
        _postRepository = postRepository;
        _groupRepository = groupRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null)
            throw new InvalidOperationException("Post not found");

        if (post.AuthorId != request.UserId)
        {
            if (post.GroupId.HasValue)
            {
                var member = await _groupRepository.GetMemberAsync(post.GroupId.Value, request.UserId, cancellationToken);
                if (member == null || (member.Role != GroupRole.Admin && member.Role != GroupRole.Moderator))
                    throw new UnauthorizedAccessException("Not authorized to delete this post");
            }
            else
            {
                throw new UnauthorizedAccessException("Not authorized to delete this post");
            }
        }

        await _postRepository.DeleteAsync(request.PostId, cancellationToken);

        _logger.LogInformation("User {UserId} deleted post {PostId}", request.UserId, request.PostId);

        return true;
    }
}

public class LikePostCommandHandler : IRequestHandler<LikePostCommand, LikeResultDto>
{
    private readonly IPostRepository _postRepository;
    private readonly ILikeService _likeService;
    private readonly ILogger<LikePostCommandHandler> _logger;

    public LikePostCommandHandler(
        IPostRepository postRepository,
        ILikeService likeService,
        ILogger<LikePostCommandHandler> logger)
    {
        _postRepository = postRepository;
        _likeService = likeService;
        _logger = logger;
    }

    public async Task<LikeResultDto> Handle(LikePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null)
            throw new InvalidOperationException("Post not found");

        var count = await _likeService.LikeAsync("post", request.PostId, request.UserId);
        _logger.LogInformation("User {UserId} liked post {PostId}, likeCount={Count}", request.UserId, request.PostId, count);

        return new LikeResultDto { LikeCount = count, UserLiked = true };
    }
}

public class UnlikePostCommandHandler : IRequestHandler<UnlikePostCommand, LikeResultDto>
{
    private readonly IPostRepository _postRepository;
    private readonly ILikeService _likeService;
    private readonly ILogger<UnlikePostCommandHandler> _logger;

    public UnlikePostCommandHandler(
        IPostRepository postRepository,
        ILikeService likeService,
        ILogger<UnlikePostCommandHandler> logger)
    {
        _postRepository = postRepository;
        _likeService = likeService;
        _logger = logger;
    }

    public async Task<LikeResultDto> Handle(UnlikePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null)
            throw new InvalidOperationException("Post not found");

        var count = await _likeService.UnlikeAsync("post", request.PostId, request.UserId);
        _logger.LogInformation("User {UserId} unliked post {PostId}, likeCount={Count}", request.UserId, request.PostId, count);

        return new LikeResultDto { LikeCount = count, UserLiked = false };
    }
}
