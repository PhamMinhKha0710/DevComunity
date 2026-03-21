using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

namespace SocialTechsy.SocialNetwork.Application.Queries.Comments;

/// <summary>
/// Query for getting comments on a post
/// </summary>
public class GetPostCommentsQuery : IRequest<IEnumerable<CommentDto>>
{
    public int PostId { get; set; }
}
