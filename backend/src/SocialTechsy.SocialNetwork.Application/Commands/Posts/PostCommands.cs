using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Application.Commands.Posts;

/// <summary>
/// Command for creating a new post
/// </summary>
public class CreatePostCommand : IRequest<PostDto>
{
    public int AuthorId { get; set; }
    public string Content { get; set; } = null!;
    public int? GroupId { get; set; }
}

/// <summary>
/// Command for updating a post
/// </summary>
public class UpdatePostCommand : IRequest<PostDto>
{
    public int PostId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = null!;
}

/// <summary>
/// Command for deleting a post
/// </summary>
public class DeletePostCommand : IRequest<bool>
{
    public int PostId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command for liking a post
/// </summary>
public class LikePostCommand : IRequest<LikeResultDto>
{
    public int PostId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// Command for unliking a post
/// </summary>
public class UnlikePostCommand : IRequest<LikeResultDto>
{
    public int PostId { get; set; }
    public int UserId { get; set; }
}
