using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Application.Queries.SavedItems;

namespace DevComunity.Application.QueryHandlers.SavedItems;

/// <summary>
/// Handler for getting saved items
/// </summary>
public class GetSavedItemsQueryHandler
{
    private readonly ISavedItemRepository _savedItemRepository;

    public GetSavedItemsQueryHandler(ISavedItemRepository savedItemRepository)
    {
        _savedItemRepository = savedItemRepository;
    }

    public async Task<(IEnumerable<SavedItemDto> Items, int TotalCount)> HandleAsync(GetSavedItemsQuery query, CancellationToken cancellationToken)
    {
        var items = await _savedItemRepository.GetByUserIdAsync(
            query.UserId, 
            query.Type, 
            query.Page, 
            query.PageSize, 
            cancellationToken);
            
        var totalCount = await _savedItemRepository.GetCountByUserIdAsync(
            query.UserId, 
            query.Type, 
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

        return (dtos, totalCount);
    }
}
