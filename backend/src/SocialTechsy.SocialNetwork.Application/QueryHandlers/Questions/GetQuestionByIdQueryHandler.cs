using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Questions;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Questions;

/// <summary>
/// Handler for GetQuestionByIdQuery
/// </summary>
public class GetQuestionByIdQueryHandler : IRequestHandler<GetQuestionByIdQuery, QuestionDto?>
{
    private readonly IQuestionRepository _questionRepository;

    public GetQuestionByIdQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<QuestionDto?> Handle(GetQuestionByIdQuery request, CancellationToken cancellationToken = default)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);
        
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
            CreatedDate = DateTime.SpecifyKind(question.CreatedDate, DateTimeKind.Utc),
            UpdatedDate = question.UpdatedDate.HasValue ? DateTime.SpecifyKind(question.UpdatedDate.Value, DateTimeKind.Utc) : null,
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
