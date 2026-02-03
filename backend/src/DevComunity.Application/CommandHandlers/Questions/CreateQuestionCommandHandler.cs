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
    private readonly IUserRepository _userRepository;

    public CreateQuestionCommandHandler(
        IQuestionRepository questionRepository,
        IUserRepository userRepository)
    {
        _questionRepository = questionRepository;
        _userRepository = userRepository;
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

        // Award reputation for asking a question (+2)
        await _userRepository.UpdateReputationAsync(
            command.UserId, 
            CommandHandlers.Votes.ReputationPoints.AskQuestion, 
            cancellationToken);

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
