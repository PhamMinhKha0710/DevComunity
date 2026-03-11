using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Questions;

/// <summary>
/// Handler for CreateQuestionCommand
/// </summary>
public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, QuestionDto>
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

    public async Task<QuestionDto> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = new Question
        {
            Title = request.Title,
            Body = request.Body,
            UserId = request.UserId,
            CreatedDate = DateTime.UtcNow,
            Status = "open",
            ViewCount = 0,
            Score = 0
        };

        var createdQuestion = await _questionRepository.AddAsync(question, cancellationToken);

        // Award reputation for asking a question (+2)
        await _userRepository.UpdateReputationAsync(
            request.UserId, 
            CommandHandlers.Votes.ReputationPoints.AskQuestion, 
            cancellationToken);

        var createdUtc = DateTime.SpecifyKind(createdQuestion.CreatedDate, DateTimeKind.Utc);

        return new QuestionDto
        {
            QuestionId = createdQuestion.QuestionId,
            Title = createdQuestion.Title,
            Body = createdQuestion.Body,
            CreatedDate = createdUtc,
            Status = createdQuestion.Status,
            ViewCount = createdQuestion.ViewCount,
            Score = createdQuestion.Score
        };
    }
}
