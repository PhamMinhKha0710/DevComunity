using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Application.Queries.Comments;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Comments;

public class GetPostCommentsQueryHandler : IRequestHandler<GetPostCommentsQuery, IEnumerable<CommentDto>>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ICacheService _cacheService;

    public GetPostCommentsQueryHandler(ICommentRepository commentRepository, ICacheService cacheService)
    {
        _commentRepository = commentRepository;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<CommentDto>> Handle(
        GetPostCommentsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"comments:post:{request.PostId}";

        return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
            var comments = await _commentRepository.GetByPostIdAsync(request.PostId, cancellationToken);

            return comments.Select(c => new CommentDto
            {
                CommentId = c.CommentId,
                Body = c.Body,
                CreatedDate = c.CreatedDate,
                UserId = c.UserId,
                AuthorId = c.UserId,
                AuthorUsername = c.User?.Username ?? "Unknown",
                AuthorProfilePicture = c.User?.ProfilePicture
            }).ToList();
        }, TimeSpan.FromMinutes(5)) ?? Enumerable.Empty<CommentDto>();
    }
}
