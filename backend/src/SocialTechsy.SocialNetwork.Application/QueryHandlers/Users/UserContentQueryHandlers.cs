using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Users;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Users;

public class GetUserQuestionsQuery : IRequest<PaginatedResponse<QuestionDto>>
{
    public int UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

public class GetUserAnswersQuery : IRequest<PaginatedResponse<AnswerDto>>
{
    public int UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

/// <summary>
/// Handler for getting a user's questions
/// </summary>
public class GetUserQuestionsQueryHandler : IRequestHandler<GetUserQuestionsQuery, PaginatedResponse<QuestionDto>>
{
    private readonly IQuestionRepository _questionRepository;

    public GetUserQuestionsQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<PaginatedResponse<QuestionDto>> Handle(
        GetUserQuestionsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _questionRepository.GetByUserIdAsync(
            request.UserId, request.Page, request.PageSize, cancellationToken);

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
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Handler for getting a user's answers
/// </summary>
public class GetUserAnswersQueryHandler : IRequestHandler<GetUserAnswersQuery, PaginatedResponse<AnswerDto>>
{
    private readonly IAnswerRepository _answerRepository;

    public GetUserAnswersQueryHandler(IAnswerRepository answerRepository)
    {
        _answerRepository = answerRepository;
    }

    public async Task<PaginatedResponse<AnswerDto>> Handle(
        GetUserAnswersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _answerRepository.GetByUserIdAsync(
            request.UserId, request.Page, request.PageSize, cancellationToken);

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
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
