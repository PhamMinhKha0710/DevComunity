using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Questions;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Questions;

/// <summary>
/// Handler for GetQuestionsQuery
/// </summary>
public class GetQuestionsQueryHandler : IRequestHandler<GetQuestionsQuery, PaginatedResponse<QuestionDto>>
{
    private readonly IQuestionRepository _questionRepository;

    public GetQuestionsQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<PaginatedResponse<QuestionDto>> Handle(GetQuestionsQuery request, CancellationToken cancellationToken = default)
    {
        var (questions, totalCount) = await _questionRepository.GetPaginatedAsync(
            request.Page,
            request.PageSize,
            request.SearchTerm,
            request.Tag,
            request.Sort,
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
            CreatedDate = DateTime.SpecifyKind(q.CreatedDate, DateTimeKind.Utc),
            UpdatedDate = q.UpdatedDate.HasValue ? DateTime.SpecifyKind(q.UpdatedDate.Value, DateTimeKind.Utc) : null,
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
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
