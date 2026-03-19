using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Application.Queries.Posts;

/// <summary>
/// Query for getting personalized newsfeed
/// </summary>
public class GetNewsfeedQuery : IRequest<PaginatedResponse<PostDto>>
{
    public int UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Query for getting posts from a specific group
/// </summary>
public class GetGroupPostsQuery : IRequest<PaginatedResponse<PostDto>>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Query for getting posts by a specific user
/// </summary>
public class GetUserPostsQuery : IRequest<PaginatedResponse<PostDto>>
{
    public int TargetUserId { get; set; }
    public int CurrentUserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Query for getting a single post
/// </summary>
public class GetPostQuery : IRequest<PostDto?>
{
    public int PostId { get; set; }
    public int? CurrentUserId { get; set; }
}
