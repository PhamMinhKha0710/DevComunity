using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;

namespace DevComunity.Application.QueryHandlers.Users;

/// <summary>
/// Handler for getting a user's questions
/// </summary>
public class GetUserQuestionsQueryHandler
{
    private readonly IQuestionRepository _questionRepository;

    public GetUserQuestionsQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<PaginatedResponse<QuestionDto>> HandleAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _questionRepository.GetByUserIdAsync(
            userId, page, pageSize, cancellationToken);

        return new PaginatedResponse<QuestionDto>
        {
            Items = items.Select(q => new QuestionDto
            {
                QuestionId = q.QuestionId,
                Title = q.Title,
                Body = q.Body,
                Score = q.Score,
                ViewCount = q.ViewCount,
                AnswerCount = q.Answers?.Count ?? 0,
                HasAcceptedAnswer = q.Answers?.Any(a => a.IsAccepted) ?? false,
                CreatedDate = q.CreatedDate,
                Status = q.Status ?? "open",
                AuthorId = q.User?.UserId,
                AuthorUsername = q.User?.Username,
                AuthorProfilePicture = q.User?.ProfilePicture,
                AuthorReputation = q.User?.ReputationPoints,
                Tags = q.QuestionTags?.Select(qt => new TagDto
                {
                    TagId = qt.Tag?.TagId ?? 0,
                    TagName = qt.Tag?.TagName ?? "",
                    Description = qt.Tag?.Description
                }).ToList() ?? new List<TagDto>()
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Handler for getting a user's answers
/// </summary>
public class GetUserAnswersQueryHandler
{
    private readonly IAnswerRepository _answerRepository;

    public GetUserAnswersQueryHandler(IAnswerRepository answerRepository)
    {
        _answerRepository = answerRepository;
    }

    public async Task<PaginatedResponse<AnswerDto>> HandleAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _answerRepository.GetByUserIdAsync(
            userId, page, pageSize, cancellationToken);

        return new PaginatedResponse<AnswerDto>
        {
            Items = items.Select(a => new AnswerDto
            {
                AnswerId = a.AnswerId,
                QuestionId = a.QuestionId,
                Body = a.Body,
                Score = a.Score,
                IsAccepted = a.IsAccepted,
                CreatedDate = a.CreatedDate,
                AuthorId = a.User?.UserId,
                AuthorUsername = a.User?.Username,
                AuthorProfilePicture = a.User?.ProfilePicture,
                AuthorReputation = a.User?.ReputationPoints
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
