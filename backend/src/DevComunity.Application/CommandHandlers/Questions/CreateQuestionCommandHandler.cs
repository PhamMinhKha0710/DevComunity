using DevComunity.Application.Commands.Questions;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;

namespace DevComunity.Application.CommandHandlers.Questions;

/// <summary>
/// Handler for CreateQuestionCommand
/// </summary>
public class CreateQuestionCommandHandler
{
    private readonly IQuestionRepository _questionRepository;

    public CreateQuestionCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<QuestionDto> HandleAsync(CreateQuestionCommand command, CancellationToken cancellationToken = default)
    {
        var question = new Question
        {
            Title = command.Title,
            Body = command.Body,
            UserId = command.UserId,
            CreatedDate = DateTime.UtcNow,
            Status = "open",
            ViewCount = 0,
            Score = 0
        };

        var createdQuestion = await _questionRepository.AddAsync(question, cancellationToken);

        return new QuestionDto
        {
            QuestionId = createdQuestion.QuestionId,
            Title = createdQuestion.Title,
            Body = createdQuestion.Body,
            CreatedDate = createdQuestion.CreatedDate,
            Status = createdQuestion.Status,
            ViewCount = createdQuestion.ViewCount,
            Score = createdQuestion.Score
        };
    }
}
