using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Application.Queries.Search;

namespace DevComunity.Application.QueryHandlers.Search;

/// <summary>
/// Handler for unified search across questions, users, and tags
/// </summary>
public class SearchQueryHandler
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITagRepository _tagRepository;

    public SearchQueryHandler(
        IQuestionRepository questionRepository,
        IUserRepository userRepository,
        ITagRepository tagRepository)
    {
        _questionRepository = questionRepository;
        _userRepository = userRepository;
        _tagRepository = tagRepository;
    }

    public async Task<SearchResultDto> HandleAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var result = new SearchResultDto();
        var searchTerm = query.Query?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(searchTerm))
            return result;

        // Search questions
        var (questions, _) = await _questionRepository.GetPaginatedAsync(
            1, query.MaxResults, searchTerm, null, "newest", cancellationToken);

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

        // Search users
        var (users, _) = await _userRepository.GetPaginatedAsync(
            1, query.MaxResults, searchTerm, "reputation", cancellationToken);

        result.Users = users.Select(u => new UserSearchResult
        {
            UserId = u.UserId,
            Username = u.Username,
            DisplayName = u.DisplayName,
            ProfilePicture = u.ProfilePicture,
            ReputationPoints = u.ReputationPoints
        }).ToList();

        // Search tags
        var (tags, _) = await _tagRepository.GetPaginatedAsync(
            1, query.MaxResults, searchTerm, "popular", cancellationToken);

        result.Tags = tags.Select(t => new TagSearchResult
        {
            TagId = t.TagId,
            TagName = t.TagName,
            Description = t.Description,
            QuestionCount = t.QuestionTags?.Count ?? 0
        }).ToList();

        return result;
    }
}
