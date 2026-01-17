using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Application.Queries.Questions;

namespace DevComunity.Application.QueryHandlers.Questions;

/// <summary>
/// Handler for GetQuestionByIdQuery
/// </summary>
public class GetQuestionByIdQueryHandler
{
    private readonly IQuestionRepository _questionRepository;

    public GetQuestionByIdQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<QuestionDto?> HandleAsync(GetQuestionByIdQuery query, CancellationToken cancellationToken = default)
    {
        var question = await _questionRepository.GetByIdAsync(query.QuestionId, cancellationToken);
        
        if (question == null)
            return null;

        return new QuestionDto
        {
            QuestionId = question.QuestionId,
            Title = question.Title,
            Body = question.Body,
            ViewCount = question.ViewCount,
            Score = question.Score,
            AnswerCount = question.Answers?.Count ?? 0,
            HasAcceptedAnswer = question.Answers?.Any(a => a.IsAccepted) ?? false,
            CreatedDate = question.CreatedDate,
            UpdatedDate = question.UpdatedDate,
            Status = question.Status,
            AuthorId = question.UserId,
            AuthorUsername = question.User?.Username,
            AuthorProfilePicture = question.User?.ProfilePicture,
            AuthorReputation = question.User?.ReputationPoints,
            Tags = question.QuestionTags?.Select(qt => new TagDto
            {
                TagId = qt.Tag.TagId,
                TagName = qt.Tag.TagName
            }).ToList() ?? new List<TagDto>()
        };
    }
}
