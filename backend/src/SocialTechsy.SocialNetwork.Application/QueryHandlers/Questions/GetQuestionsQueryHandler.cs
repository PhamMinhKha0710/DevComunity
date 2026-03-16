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
    private readonly ICacheService? _cacheService;

    private const int CACHE_TTL_SECONDS = 60;

    public GetQuestionsQueryHandler(
        IQuestionRepository questionRepository,
        IVoteRepository voteRepository,
        ILikeService? likeService = null,
        IViewService? viewService = null,
        ICacheService? cacheService = null)
    {
        _questionRepository = questionRepository;
        _voteRepository = voteRepository;
        _likeService = likeService;
        _viewService = viewService;
        _cacheService = cacheService;
    }

    public async Task<PaginatedResponse<QuestionSummaryDto>> Handle(GetQuestionsQuery request, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"questions:page:{request.Page}:size:{request.PageSize}:sort:{request.Sort}:tag:{request.Tag ?? "none"}:term:{request.SearchTerm ?? "none"}";

        // Try to get from cache
        if (_cacheService != null && string.IsNullOrEmpty(request.SearchTerm))
        {
            var cached = await _cacheService.GetOrCreateAsync<PaginatedResponse<QuestionSummaryDto>>(
                cacheKey,
                async () => await GetQuestionsFromDbAsync(request, cancellationToken),
                TimeSpan.FromSeconds(CACHE_TTL_SECONDS));

            if (cached != null)
                return cached;
        }

        // Fallback to DB
        return await GetQuestionsFromDbAsync(request, cancellationToken);
    }

    private async Task<PaginatedResponse<QuestionSummaryDto>> GetQuestionsFromDbAsync(GetQuestionsQuery request, CancellationToken cancellationToken)
    {
        // Use optimized query (no Answers include, AsNoTracking)
        var (questions, totalCount) = await _questionRepository.GetPaginatedOptimizedAsync(
            request.Page, request.PageSize, request.SearchTerm, request.Tag, request.Sort, cancellationToken);

        var items = questions.Select(q => q.ToSummaryDto()).ToList();
        var ids = items.Select(i => i.QuestionId).ToArray();

        // Get like counts in parallel
        if (_likeService != null && ids.Length > 0)
        {
            var likeCounts = await _likeService.GetLikeCountsBatchAsync("question", ids);
            foreach (var item in items)
            {
                item.Score = likeCounts.TryGetValue(item.QuestionId, out var c) ? (int)c : item.Score;
            }
        }
        else if (ids.Length > 0)
        {
            // Fallback: get scores sequentially (slower)
            foreach (var item in items)
                item.Score = await _voteRepository.GetQuestionScoreAsync(item.QuestionId, cancellationToken);
        }

        // Get view counts in parallel
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
