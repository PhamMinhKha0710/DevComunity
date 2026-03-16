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
    private readonly ICacheService? _cacheService;

    private const int CACHE_TTL_SECONDS = 300; // 5 minutes

    public GetQuestionByIdQueryHandler(
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

    public async Task<QuestionDetailDto?> Handle(GetQuestionByIdQuery request, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"question:{request.QuestionId}";

        // Try to get from cache
        if (_cacheService != null)
        {
            var cached = await _cacheService.GetOrCreateAsync<QuestionDetailDto?>(
                cacheKey,
                async () => await GetQuestionFromDbAsync(request, cancellationToken),
                TimeSpan.FromSeconds(CACHE_TTL_SECONDS));

            // If cached and user is logged in, check vote status
            if (cached != null && request.CurrentUserId.HasValue && request.CurrentUserId.Value > 0)
            {
                return await EnrichWithUserVoteAsync(cached, request.CurrentUserId.Value, cancellationToken);
            }

            return cached;
        }

        // Fallback to DB
        return await GetQuestionFromDbAsync(request, cancellationToken);
    }

    private async Task<QuestionDetailDto?> GetQuestionFromDbAsync(GetQuestionByIdQuery request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);
        if (question == null) return null;

        var dto = question.ToDetailDto();

        // Get score
        dto.Score = _likeService != null
            ? (int)await _likeService.GetLikeCountAsync("question", request.QuestionId)
            : await _voteRepository.GetQuestionScoreAsync(request.QuestionId, cancellationToken);

        // Get view count
        if (_viewService != null)
        {
            var views = await _viewService.GetViewCountAsync(request.QuestionId);
            if (views > 0) dto.ViewCount = (int)views;
        }

        // Get user vote if logged in
        if (request.CurrentUserId.HasValue && request.CurrentUserId.Value > 0)
        {
            return await EnrichWithUserVoteAsync(dto, request.CurrentUserId.Value, cancellationToken);
        }

        return dto;
    }

    private async Task<QuestionDetailDto> EnrichWithUserVoteAsync(QuestionDetailDto dto, int currentUserId, CancellationToken cancellationToken)
    {
        if (_likeService != null)
        {
            var isLiked = await _likeService.IsLikedAsync("question", dto.QuestionId, currentUserId);
            dto.UserVoteType = isLiked ? "up" : null;
        }
        else
        {
            var vote = await _voteRepository.GetUserVoteOnQuestionAsync(currentUserId, dto.QuestionId, cancellationToken);
            dto.UserVoteType = vote == null ? null : (vote.IsUpvote ? "up" : "down");
        }

        return dto;
    }
}
