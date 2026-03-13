using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Application.Queries.Questions;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Questions;

public class GetQuestionsQueryHandler : IRequestHandler<GetQuestionsQuery, PaginatedResponse<QuestionSummaryDto>>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly ILikeService? _likeService;
    private readonly IViewService? _viewService;

    public GetQuestionsQueryHandler(
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

    public async Task<PaginatedResponse<QuestionSummaryDto>> Handle(GetQuestionsQuery request, CancellationToken cancellationToken = default)
    {
        var (questions, totalCount) = await _questionRepository.GetPaginatedAsync(
            request.Page, request.PageSize, request.SearchTerm, request.Tag, request.Sort, cancellationToken);

        var items = questions.Select(q => q.ToSummaryDto()).ToList();
        var ids = items.Select(i => i.QuestionId).ToArray();

        if (_likeService != null && ids.Length > 0)
        {
            var likeCounts = await _likeService.GetLikeCountsBatchAsync("question", ids);
            foreach (var item in items)
            {
                item.Score = likeCounts.TryGetValue(item.QuestionId, out var c) ? (int)c : item.Score;
            }
        }
        else
        {
            foreach (var item in items)
                item.Score = await _voteRepository.GetQuestionScoreAsync(item.QuestionId, cancellationToken);
        }

        if (_viewService != null && ids.Length > 0)
        {
            var viewCounts = await _viewService.GetViewCountsBatchAsync(ids);
            foreach (var item in items)
            {
                if (viewCounts.TryGetValue(item.QuestionId, out var v) && v > 0)
                    item.ViewCount = (int)v;
            }
        }

        return new PaginatedResponse<QuestionSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
