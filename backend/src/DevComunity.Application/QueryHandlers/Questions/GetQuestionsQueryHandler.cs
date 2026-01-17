using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Application.Queries.Questions;

namespace DevComunity.Application.QueryHandlers.Questions;

/// <summary>
/// Handler for GetQuestionsQuery
/// </summary>
public class GetQuestionsQueryHandler
{
    private readonly IQuestionRepository _questionRepository;

    public GetQuestionsQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<PaginatedResponse<QuestionDto>> HandleAsync(GetQuestionsQuery query, CancellationToken cancellationToken = default)
    {
        var (questions, totalCount) = await _questionRepository.GetPaginatedAsync(
            query.Page,
            query.PageSize,
            query.SearchTerm,
            query.Tag,
            query.Sort,
            cancellationToken);

        var items = questions.Select(q => new QuestionDto
        {
            QuestionId = q.QuestionId,
            Title = q.Title,
            Body = q.Body,
            BodyExcerpt = q.Body.Length > 200 ? q.Body.Substring(0, 200) + "..." : q.Body,
            ViewCount = q.ViewCount,
            Score = q.Score,
            AnswerCount = q.Answers?.Count ?? 0,
            HasAcceptedAnswer = q.Answers?.Any(a => a.IsAccepted) ?? false,
            CreatedDate = q.CreatedDate,
            UpdatedDate = q.UpdatedDate,
            Status = q.Status,
            AuthorId = q.UserId,
            AuthorUsername = q.User?.Username,
            AuthorProfilePicture = q.User?.ProfilePicture,
            AuthorReputation = q.User?.ReputationPoints,
            Tags = q.QuestionTags?.Select(qt => new TagDto
            {
                TagId = qt.Tag.TagId,
                TagName = qt.Tag.TagName
            }).ToList() ?? new List<TagDto>()
        }).ToList();

        return new PaginatedResponse<QuestionDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
