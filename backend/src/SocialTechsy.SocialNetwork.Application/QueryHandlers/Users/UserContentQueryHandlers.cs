using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Users;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Users;

public class GetUserQuestionsQuery : IRequest<PaginatedResponse<QuestionSummaryDto>>
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
public class GetUserQuestionsQueryHandler : IRequestHandler<GetUserQuestionsQuery, PaginatedResponse<QuestionSummaryDto>>
{
    private readonly IQuestionRepository _questionRepository;

    public GetUserQuestionsQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<PaginatedResponse<QuestionSummaryDto>> Handle(
        GetUserQuestionsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _questionRepository.GetByUserIdAsync(
            request.UserId, request.Page, request.PageSize, cancellationToken);

        return new PaginatedResponse<QuestionSummaryDto>
        {
            Items = items.Select(q => q.ToSummaryDto()).ToList(),
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
            Items = items.Select(a => a.ToDto()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
