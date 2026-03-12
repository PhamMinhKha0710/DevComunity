using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Application.Queries.Questions;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Questions;

public class GetQuestionByIdQueryHandler : IRequestHandler<GetQuestionByIdQuery, QuestionDetailDto?>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly ILikeService? _likeService;
    private readonly IViewService? _viewService;

    public GetQuestionByIdQueryHandler(
        IQuestionRepository questionRepository,
        IVoteRepository voteRepository,
        ILikeService? likeService = null,
        IViewService? viewService = null)
    {
        _questionRepository = questionRepository;
        _voteRepository = voteRepository;
        _likeService = likeService;
        _viewService = viewService;
    }

    public async Task<QuestionDetailDto?> Handle(GetQuestionByIdQuery request, CancellationToken cancellationToken = default)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);
        if (question == null) return null;

        var dto = question.ToDetailDto();

        dto.Score = _likeService != null
            ? (int)await _likeService.GetLikeCountAsync("question", request.QuestionId)
            : await _voteRepository.GetQuestionScoreAsync(request.QuestionId, cancellationToken);

        if (_viewService != null)
        {
            var views = await _viewService.GetViewCountAsync(request.QuestionId);
            if (views > 0) dto.ViewCount = (int)views;
        }

        if (request.CurrentUserId is > 0)
        {
            if (_likeService != null)
            {
                var isLiked = await _likeService.IsLikedAsync("question", request.QuestionId, request.CurrentUserId.Value);
                dto.UserVoteType = isLiked ? "up" : null;
            }
            else
            {
                var vote = await _voteRepository.GetUserVoteOnQuestionAsync(request.CurrentUserId.Value, request.QuestionId, cancellationToken);
                dto.UserVoteType = vote == null ? null : (vote.IsUpvote ? "up" : "down");
            }
        }

        return dto;
    }
}
