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
    private readonly ISavedItemRepository? _savedItemRepository;
    private readonly ILikeService? _likeService;
    private readonly IViewService? _viewService;
    private readonly ICacheService? _cacheService;

    private const int CACHE_TTL_SECONDS = 300; // 5 minutes

    public GetQuestionByIdQueryHandler(
        IQuestionRepository questionRepository,
        IVoteRepository voteRepository,
        ISavedItemRepository? savedItemRepository = null,
        ILikeService? likeService = null,
        IViewService? viewService = null,
        ICacheService? cacheService = null)
    {
        _questionRepository = questionRepository;
        _voteRepository = voteRepository;
        _savedItemRepository = savedItemRepository;
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
                return await EnrichWithUserStateAsync(cached, request.CurrentUserId.Value, cancellationToken);
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

        // Get score (like count) — ensure non-negative; new questions have 0 likes
        var rawScore = _likeService != null
            ? (int)await _likeService.GetLikeCountAsync("question", request.QuestionId)
            : await _voteRepository.GetQuestionScoreAsync(request.QuestionId, cancellationToken);
        dto.Score = Math.Max(0, rawScore);

        // Get view count
        if (_viewService != null)
        {
            var views = await _viewService.GetViewCountAsync(request.QuestionId);
            if (views > 0) dto.ViewCount = (int)views;
        }

        // Get user vote if logged in
        if (request.CurrentUserId.HasValue && request.CurrentUserId.Value > 0)
        {
            return await EnrichWithUserStateAsync(dto, request.CurrentUserId.Value, cancellationToken);
        }

        return dto;
    }

    private async Task<QuestionDetailDto> EnrichWithUserStateAsync(QuestionDetailDto dto, int currentUserId, CancellationToken cancellationToken)
    {
        // Get user vote status
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

        // Get saved status
        if (_savedItemRepository != null)
        {
            dto.IsSaved = await _savedItemRepository.IsSavedAsync(currentUserId, dto.QuestionId, null, null, cancellationToken);
        }

        return dto;
    }
}
