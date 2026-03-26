using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
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

public class GetUserQuestionsQueryHandler : IRequestHandler<GetUserQuestionsQuery, PaginatedResponse<QuestionSummaryDto>>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly ILikeService? _likeService;

    public GetUserQuestionsQueryHandler(
        IQuestionRepository questionRepository,
        IVoteRepository voteRepository,
        ILikeService? likeService = null)
    {
        _questionRepository = questionRepository;
        _voteRepository = voteRepository;
        _likeService = likeService;
    }

    public async Task<PaginatedResponse<QuestionSummaryDto>> Handle(
        GetUserQuestionsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _questionRepository.GetByUserIdAsync(
            request.UserId, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(q => q.ToSummaryDto()).ToList();
        var ids = dtos.Select(d => d.QuestionId).ToArray();

        if (_likeService != null && ids.Length > 0)
        {
            var counts = await _likeService.GetLikeCountsBatchAsync("question", ids);
            foreach (var dto in dtos)
                dto.Score = counts.TryGetValue(dto.QuestionId, out var c) ? (int)c : 0;
        }
        else
        {
            foreach (var dto in dtos)
                dto.Score = await _voteRepository.GetQuestionScoreAsync(dto.QuestionId, cancellationToken);
        }

        return new PaginatedResponse<QuestionSummaryDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

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
