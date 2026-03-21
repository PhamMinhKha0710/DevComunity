using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Application.Queries.Posts;
using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Posts;

public class GetNewsfeedQueryHandler : IRequestHandler<GetNewsfeedQuery, PaginatedResponse<PostDto>>
{
    private readonly IPostRepository _postRepository;
    private readonly ILikeService _likeService;
    private readonly ICommentRepository _commentRepository;
    private readonly ISavedItemRepository _savedItemRepository;

    public GetNewsfeedQueryHandler(
        IPostRepository postRepository,
        ILikeService likeService,
        ICommentRepository commentRepository,
        ISavedItemRepository savedItemRepository)
    {
        _postRepository = postRepository;
        _likeService = likeService;
        _commentRepository = commentRepository;
        _savedItemRepository = savedItemRepository;
    }

    public async Task<PaginatedResponse<PostDto>> Handle(GetNewsfeedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _postRepository.GetNewsfeedAsync(
            request.UserId, request.Page, request.PageSize, request.Filter, cancellationToken);

        var dtos = await EnrichWithEngagementAsync(items, request.UserId, cancellationToken);

        return new PaginatedResponse<PostDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private async Task<List<PostDto>> EnrichWithEngagementAsync(
        IEnumerable<Domain.Entities.Post> posts,
        int userId,
        CancellationToken cancellationToken)
    {
        var list = posts.ToList();
        if (list.Count == 0) return new List<PostDto>();

        var postIds = list.Select(p => p.PostId).ToArray();
        var likeCounts = await _likeService.GetLikeCountsBatchAsync("post", postIds);

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

    private static PostDto MapToDto(Domain.Entities.Post p) => new()
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

public class GetGroupPostsQueryHandler : IRequestHandler<GetGroupPostsQuery, PaginatedResponse<PostDto>>
{
    private readonly IPostRepository _postRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ILikeService _likeService;
    private readonly ICommentRepository _commentRepository;
    private readonly ISavedItemRepository _savedItemRepository;

    public GetGroupPostsQueryHandler(
        IPostRepository postRepository,
        IGroupRepository groupRepository,
        ILikeService likeService,
        ICommentRepository commentRepository,
        ISavedItemRepository savedItemRepository)
    {
        _postRepository = postRepository;
        _groupRepository = groupRepository;
        _likeService = likeService;
        _commentRepository = commentRepository;
        _savedItemRepository = savedItemRepository;
    }

    public async Task<PaginatedResponse<PostDto>> Handle(GetGroupPostsQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new InvalidOperationException($"Group with ID {request.GroupId} not found");

        if (group.IsPrivate)
        {
            var isMember = await _groupRepository.IsMemberAsync(request.GroupId, request.UserId, cancellationToken);
            if (!isMember)
                throw new UnauthorizedAccessException("User is not a member of this private group");
        }

        var (items, totalCount) = await _postRepository.GetGroupPostsAsync(
            request.GroupId, request.Page, request.PageSize, cancellationToken);

        var dtos = await EnrichWithEngagementAsync(items, request.UserId, cancellationToken);

        return new PaginatedResponse<PostDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private async Task<List<PostDto>> EnrichWithEngagementAsync(
        IEnumerable<Domain.Entities.Post> posts,
        int userId,
        CancellationToken cancellationToken)
    {
        var list = posts.ToList();
        if (list.Count == 0) return new List<PostDto>();

        var postIds = list.Select(p => p.PostId).ToArray();
        var likeCounts = await _likeService.GetLikeCountsBatchAsync("post", postIds);

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

    private static PostDto MapToDto(Domain.Entities.Post p) => new()
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

public class GetUserPostsQueryHandler : IRequestHandler<GetUserPostsQuery, PaginatedResponse<PostDto>>
{
    private readonly IPostRepository _postRepository;
    private readonly ILikeService _likeService;
    private readonly ICommentRepository _commentRepository;
    private readonly ISavedItemRepository _savedItemRepository;

    public GetUserPostsQueryHandler(
        IPostRepository postRepository,
        ILikeService likeService,
        ICommentRepository commentRepository,
        ISavedItemRepository savedItemRepository)
    {
        _postRepository = postRepository;
        _likeService = likeService;
        _commentRepository = commentRepository;
        _savedItemRepository = savedItemRepository;
    }

    public async Task<PaginatedResponse<PostDto>> Handle(GetUserPostsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _postRepository.GetUserPostsAsync(
            request.TargetUserId, request.Page, request.PageSize, cancellationToken);

        var publicPosts = items.Where(p => p.GroupId == null).ToList();
        var dtos = await EnrichWithEngagementAsync(publicPosts, request.CurrentUserId, cancellationToken);

        return new PaginatedResponse<PostDto>
        {
            Items = dtos,
            TotalCount = publicPosts.Count,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private async Task<List<PostDto>> EnrichWithEngagementAsync(
        IEnumerable<Domain.Entities.Post> posts,
        int userId,
        CancellationToken cancellationToken)
    {
        var list = posts.ToList();
        if (list.Count == 0) return new List<PostDto>();

        var postIds = list.Select(p => p.PostId).ToArray();
        var likeCounts = await _likeService.GetLikeCountsBatchAsync("post", postIds);

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

    private static PostDto MapToDto(Domain.Entities.Post p) => new()
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

public class GetPostQueryHandler : IRequestHandler<GetPostQuery, PostDto?>
{
    private readonly IPostRepository _postRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ILikeService _likeService;
    private readonly ICommentRepository _commentRepository;
    private readonly ISavedItemRepository _savedItemRepository;

    public GetPostQueryHandler(
        IPostRepository postRepository,
        IGroupRepository groupRepository,
        ILikeService likeService,
        ICommentRepository commentRepository,
        ISavedItemRepository savedItemRepository)
    {
        _postRepository = postRepository;
        _groupRepository = groupRepository;
        _likeService = likeService;
        _commentRepository = commentRepository;
        _savedItemRepository = savedItemRepository;
    }

    public async Task<PostDto?> Handle(GetPostQuery request, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
        if (post == null) return null;

        if (post.GroupId.HasValue && post.Group?.IsPrivate == true)
        {
            if (request.CurrentUserId == null)
                throw new UnauthorizedAccessException("Authentication required for private group posts");

            var isMember = await _groupRepository.IsMemberAsync(post.GroupId.Value, request.CurrentUserId.Value, cancellationToken);
            if (!isMember)
                throw new UnauthorizedAccessException("User is not a member of this private group");
        }

        var userId = request.CurrentUserId ?? 0;
        var dtos = await EnrichWithEngagementAsync(new[] { post }, userId, cancellationToken);
        return dtos[0];
    }

    private async Task<List<PostDto>> EnrichWithEngagementAsync(
        IEnumerable<Domain.Entities.Post> posts,
        int userId,
        CancellationToken cancellationToken)
    {
        var list = posts.ToList();
        if (list.Count == 0) return new List<PostDto>();

        var postIds = list.Select(p => p.PostId).ToArray();
        var likeCounts = await _likeService.GetLikeCountsBatchAsync("post", postIds);

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

    private static PostDto MapToDto(Domain.Entities.Post p) => new()
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
