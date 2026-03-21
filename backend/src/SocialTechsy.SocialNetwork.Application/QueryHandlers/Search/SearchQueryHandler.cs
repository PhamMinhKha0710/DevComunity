using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Tag;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Application.Queries.Search;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Search;

/// <summary>
/// Handler for unified search across questions, users, and tags
/// </summary>
public class SearchQueryHandler : IRequestHandler<SearchQuery, SearchResultDto>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ICacheService? _cacheService;

    private const int CACHE_TTL_SECONDS = 120; // 2 minutes

    public SearchQueryHandler(
        IQuestionRepository questionRepository,
        IUserRepository userRepository,
        ITagRepository tagRepository,
        ICacheService? cacheService = null)
    {
        _questionRepository = questionRepository;
        _userRepository = userRepository;
        _tagRepository = tagRepository;
        _cacheService = cacheService;
    }

    public async Task<SearchResultDto> Handle(SearchQuery request, CancellationToken cancellationToken)
    {
        var searchTerm = request.Query?.Trim() ?? "";

        // Empty query returns empty result
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new SearchResultDto();
        }

        // Skip cache for very short queries - search directly
        return await SearchFromDbAsync(request, cancellationToken);
    }

    private async Task<SearchResultDto> SearchFromDbAsync(SearchQuery request, CancellationToken cancellationToken)
    {
        var result = new SearchResultDto();
        var searchTerm = request.Query?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(searchTerm))
            return result;

        try
        {
            // Run all three searches in parallel
            var questionsTask = _questionRepository.GetPaginatedAsync(
                1, request.MaxResults, searchTerm, null, "newest", cancellationToken);
            var usersTask = _userRepository.GetPaginatedAsync(
                1, request.MaxResults, searchTerm, "reputation", cancellationToken);
            var tagsTask = _tagRepository.GetPaginatedAsync(
                1, request.MaxResults, searchTerm, "popular", cancellationToken);

            await Task.WhenAll(questionsTask, usersTask, tagsTask);

            var (questions, _) = await questionsTask;
            var (users, _) = await usersTask;
            var (tags, _) = await tagsTask;

            result.Questions = questions.Select(q => new QuestionSearchResult
            {
                QuestionId = q.QuestionId,
                Title = q.Title,
                Score = q.Score,
                AnswerCount = q.Answers?.Count ?? 0,
                ViewCount = q.ViewCount,
                AuthorName = q.User?.DisplayName ?? q.User?.Username ?? "Unknown",
                CreatedDate = q.CreatedDate,
                Tags = q.QuestionTags?.Select(qt => qt.Tag?.TagName ?? "").Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new List<string>()
            }).ToList();

            result.Users = users.Select(u => new UserSearchResult
            {
                UserId = u.UserId,
                Username = u.Username,
                DisplayName = u.DisplayName,
                ProfilePicture = u.ProfilePicture,
                ReputationPoints = u.ReputationPoints
            }).ToList();

            result.Tags = tags.Select(t => new TagSearchResult
            {
                TagId = t.TagId,
                TagName = t.TagName,
                Description = t.Description,
                QuestionCount = t.QuestionTags?.Count ?? 0
            }).ToList();
        }
        catch (Exception ex)
        {
            // Log error but return empty result
            Console.WriteLine($"Search error: {ex.Message}");
        }

        return result;
    }

    private static string GetCacheKeyHash(string query, int maxResults)
    {
        // Simple hash for cache key
        return $"{query.ToLowerInvariant()}_{maxResults}".GetHashCode().ToString("X8");
    }
}
