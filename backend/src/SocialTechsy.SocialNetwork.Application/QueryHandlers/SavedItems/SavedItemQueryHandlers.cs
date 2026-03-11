using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.SavedItems;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.SavedItems;

/// <summary>
/// Handler for getting saved items
/// </summary>
public class GetSavedItemsQueryHandler : IRequestHandler<GetSavedItemsQuery, SavedItemsResponse>
{
    private readonly ISavedItemRepository _savedItemRepository;

    public GetSavedItemsQueryHandler(ISavedItemRepository savedItemRepository)
    {
        _savedItemRepository = savedItemRepository;
    }

    public async Task<SavedItemsResponse> Handle(GetSavedItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _savedItemRepository.GetByUserIdAsync(
            request.UserId, 
            request.Type, 
            request.Page, 
            request.PageSize, 
            cancellationToken);
            
        var totalCount = await _savedItemRepository.GetCountByUserIdAsync(
            request.UserId, 
            request.Type, 
            cancellationToken);

        var dtos = items.Select(s => new SavedItemDto
        {
            SavedItemId = s.SavedItemId,
            CreatedDate = s.CreatedDate,
            Type = s.QuestionId != null ? "question" : "answer",
            QuestionId = s.QuestionId,
            QuestionTitle = s.Question?.Title,
            QuestionScore = s.Question?.Score,
            QuestionAnswerCount = s.Question?.Answers?.Count,
            AnswerId = s.AnswerId,
            AnswerBody = s.Answer?.Body != null 
                ? (s.Answer.Body.Length > 150 ? s.Answer.Body.Substring(0, 150) + "..." : s.Answer.Body) 
                : null,
            AnswerScore = s.Answer?.Score,
            RelatedQuestionId = s.Answer?.QuestionId,
            RelatedQuestionTitle = s.Answer?.Question?.Title
        });

        return new SavedItemsResponse { Items = dtos, TotalCount = totalCount };
    }
}
