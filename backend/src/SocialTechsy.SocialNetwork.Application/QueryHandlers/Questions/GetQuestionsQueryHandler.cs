using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Application.Queries.Questions;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Questions;

/// <summary>
/// Handler for GetQuestionsQuery
/// </summary>
public class GetQuestionsQueryHandler : IRequestHandler<GetQuestionsQuery, PaginatedResponse<QuestionSummaryDto>>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ICacheService _cacheService;

    public GetQuestionsQueryHandler(
        IQuestionRepository questionRepository,
        ICacheService cacheService)
    {
        _questionRepository = questionRepository;
        _cacheService = cacheService;
    }

    public async Task<PaginatedResponse<QuestionSummaryDto>> Handle(GetQuestionsQuery request, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"questions_{request.Page}_{request.PageSize}_{request.SearchTerm ?? ""}_{request.Tag ?? ""}_{request.Sort}";

        return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
            var (questions, totalCount) = await _questionRepository.GetPaginatedAsync(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                request.Tag,
                request.Sort,
                cancellationToken);

            var items = questions.Select(q => q.ToSummaryDto()).ToList();

            return new PaginatedResponse<QuestionSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }, TimeSpan.FromMinutes(2));
    }
}
