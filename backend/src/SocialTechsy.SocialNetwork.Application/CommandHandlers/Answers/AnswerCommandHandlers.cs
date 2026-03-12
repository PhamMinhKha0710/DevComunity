using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Answers;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Events;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Answers;

/// <summary>
/// Handler for CreateAnswerCommand
/// </summary>
public class CreateAnswerCommandHandler : IRequestHandler<CreateAnswerCommand, AnswerDto?>
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

    public async Task<AnswerDto?> Handle(CreateAnswerCommand request, CancellationToken cancellationToken)
    {
        // Verify question exists
        if (!await _questionRepository.ExistsAsync(request.QuestionId, cancellationToken))
            return null;

        var answer = new Answer
        {
            QuestionId = request.QuestionId,
            Body = request.Body,
            UserId = request.UserId,
            ParentAnswerId = request.ParentAnswerId,
            CreatedDate = DateTime.UtcNow,
            IsAccepted = false,
            Score = 0
        };

        var createdAnswer = await _answerRepository.AddAsync(answer, cancellationToken);

        // Ensure CreatedDate is marked as UTC when serialized
        return createdAnswer.ToDto();
    }
}

/// <summary>
/// Handler for UpdateAnswerCommand
/// </summary>
public class UpdateAnswerCommandHandler : IRequestHandler<UpdateAnswerCommand, bool>
{
    private readonly IAnswerRepository _answerRepository;

    public UpdateAnswerCommandHandler(IAnswerRepository answerRepository)
    {
        _answerRepository = answerRepository;
    }

    public async Task<bool> Handle(UpdateAnswerCommand request, CancellationToken cancellationToken)
    {
        var answer = await _answerRepository.GetByIdAsync(request.AnswerId, cancellationToken);
        
        if (answer == null || answer.UserId != request.UserId)
            return false;

        answer.Body = request.Body;
        answer.UpdatedDate = DateTime.UtcNow;

        await _answerRepository.UpdateAsync(answer, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for DeleteAnswerCommand
/// </summary>
public class DeleteAnswerCommandHandler : IRequestHandler<DeleteAnswerCommand, bool>
{
    private readonly IAnswerRepository _answerRepository;

    public DeleteAnswerCommandHandler(IAnswerRepository answerRepository)
    {
        _answerRepository = answerRepository;
    }

    public async Task<bool> Handle(DeleteAnswerCommand request, CancellationToken cancellationToken)
    {
        var answer = await _answerRepository.GetByIdAsync(request.AnswerId, cancellationToken);
        
        if (answer == null || answer.UserId != request.UserId)
            return false;

        await _answerRepository.DeleteAsync(request.AnswerId, cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for AcceptAnswerCommand
/// </summary>
public class AcceptAnswerCommandHandler : IRequestHandler<AcceptAnswerCommand, AcceptAnswerResult>
{
    private readonly IAnswerRepository _answerRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public AcceptAnswerCommandHandler(
        IAnswerRepository answerRepository,
        IQuestionRepository questionRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _answerRepository = answerRepository;
        _questionRepository = questionRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<AcceptAnswerResult> Handle(AcceptAnswerCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);
        if (question == null || question.UserId != request.UserId)
            return new AcceptAnswerResult { Success = false, Message = "Not authorized to accept answers for this question" };

        var answer = await _answerRepository.GetByIdAsync(request.AnswerId, cancellationToken);
        if (answer == null)
            return new AcceptAnswerResult { Success = false, Message = "Answer not found" };

        if (answer.IsAccepted)
            return new AcceptAnswerResult { Success = true, Message = "Answer already accepted" };

        var success = await _answerRepository.AcceptAnswerAsync(request.AnswerId, request.QuestionId, cancellationToken);

        if (success)
        {
            await _eventDispatcher.DispatchAsync(new AnswerAcceptedEvent
            {
                AnswerId = request.AnswerId,
                QuestionId = request.QuestionId,
                AnswerAuthorId = answer.UserId,
                QuestionOwnerId = question.UserId,
                QuestionTitle = question.Title
            }, cancellationToken);
        }

        return new AcceptAnswerResult
        {
            Success = success,
            Message = success ? "Answer accepted successfully" : null
        };
    }
}

public class AcceptAnswerResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
