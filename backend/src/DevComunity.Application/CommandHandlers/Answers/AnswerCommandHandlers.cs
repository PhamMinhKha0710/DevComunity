using DevComunity.Application.Commands.Answers;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;

namespace DevComunity.Application.CommandHandlers.Answers;

/// <summary>
/// Handler for CreateAnswerCommand
/// </summary>
public class CreateAnswerCommandHandler
{
    private readonly IAnswerRepository _answerRepository;
    private readonly IQuestionRepository _questionRepository;

    public CreateAnswerCommandHandler(
        IAnswerRepository answerRepository,
        IQuestionRepository questionRepository)
    {
        _answerRepository = answerRepository;
        _questionRepository = questionRepository;
    }

    public async Task<AnswerDto?> HandleAsync(CreateAnswerCommand command, CancellationToken cancellationToken = default)
    {
        // Verify question exists
        if (!await _questionRepository.ExistsAsync(command.QuestionId, cancellationToken))
            return null;

        var answer = new Answer
        {
            QuestionId = command.QuestionId,
            Body = command.Body,
            UserId = command.UserId,
            ParentAnswerId = command.ParentAnswerId,
            CreatedDate = DateTime.UtcNow,
            IsAccepted = false,
            Score = 0
        };

        var createdAnswer = await _answerRepository.AddAsync(answer, cancellationToken);

        return new AnswerDto
        {
            AnswerId = createdAnswer.AnswerId,
            QuestionId = createdAnswer.QuestionId,
            Body = createdAnswer.Body,
            Score = createdAnswer.Score,
            IsAccepted = createdAnswer.IsAccepted,
            CreatedDate = createdAnswer.CreatedDate,
            AuthorId = createdAnswer.UserId,
            ParentAnswerId = createdAnswer.ParentAnswerId
        };
    }
}

/// <summary>
/// Handler for UpdateAnswerCommand
/// </summary>
public class UpdateAnswerCommandHandler
{
    private readonly IAnswerRepository _answerRepository;

    public UpdateAnswerCommandHandler(IAnswerRepository answerRepository)
    {
        _answerRepository = answerRepository;
    }

    public async Task<bool> HandleAsync(UpdateAnswerCommand command, CancellationToken cancellationToken = default)
    {
        var answer = await _answerRepository.GetByIdAsync(command.AnswerId, cancellationToken);
        
        if (answer == null || answer.UserId != command.UserId)
            return false;

        answer.Body = command.Body;
        answer.UpdatedDate = DateTime.UtcNow;

        await _answerRepository.UpdateAsync(answer, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for DeleteAnswerCommand
/// </summary>
public class DeleteAnswerCommandHandler
{
    private readonly IAnswerRepository _answerRepository;

    public DeleteAnswerCommandHandler(IAnswerRepository answerRepository)
    {
        _answerRepository = answerRepository;
    }

    public async Task<bool> HandleAsync(DeleteAnswerCommand command, CancellationToken cancellationToken = default)
    {
        var answer = await _answerRepository.GetByIdAsync(command.AnswerId, cancellationToken);
        
        if (answer == null || answer.UserId != command.UserId)
            return false;

        await _answerRepository.DeleteAsync(command.AnswerId, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for AcceptAnswerCommand
/// </summary>
public class AcceptAnswerCommandHandler
{
    private readonly IAnswerRepository _answerRepository;
    private readonly IQuestionRepository _questionRepository;

    public AcceptAnswerCommandHandler(
        IAnswerRepository answerRepository,
        IQuestionRepository questionRepository)
    {
        _answerRepository = answerRepository;
        _questionRepository = questionRepository;
    }

    public async Task<bool> HandleAsync(AcceptAnswerCommand command, CancellationToken cancellationToken = default)
    {
        // Verify question belongs to user
        var question = await _questionRepository.GetByIdAsync(command.QuestionId, cancellationToken);
        if (question == null || question.UserId != command.UserId)
            return false;

        return await _answerRepository.AcceptAnswerAsync(command.AnswerId, command.QuestionId, cancellationToken);
    }
}
