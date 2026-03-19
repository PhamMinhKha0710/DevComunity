using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Answers;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
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
    private readonly IUnitOfWork _unitOfWork;

    public CreateAnswerCommandHandler(
        IAnswerRepository answerRepository,
        IQuestionRepository questionRepository,
        IUnitOfWork unitOfWork)
    {
        _answerRepository = answerRepository;
        _questionRepository = questionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AnswerDto?> Handle(CreateAnswerCommand request, CancellationToken cancellationToken)
    {
        if (!await _questionRepository.ExistsAsync(request.QuestionId, cancellationToken))
            return null;

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var answer = Answer.Create(request.QuestionId, request.UserId, request.Body, request.ParentAnswerId);
            var createdAnswer = await _answerRepository.AddAsync(answer, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return createdAnswer.ToDto();
        }, cancellationToken);
    }
}

/// <summary>
/// Handler for UpdateAnswerCommand
/// </summary>
public class UpdateAnswerCommandHandler : IRequestHandler<UpdateAnswerCommand, bool>
{
    private readonly IAnswerRepository _answerRepository;
    private readonly IQuestionEventDispatcher _dispatcher;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAnswerCommandHandler(
        IAnswerRepository answerRepository,
        IQuestionEventDispatcher dispatcher,
        IUnitOfWork unitOfWork)
    {
        _answerRepository = answerRepository;
        _dispatcher = dispatcher;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateAnswerCommand request, CancellationToken cancellationToken)
    {
        var answer = await _answerRepository.GetByIdAsync(request.AnswerId, cancellationToken);

        if (answer == null || answer.UserId != request.UserId)
            return false;

        answer.Update(request.Body);

        await _answerRepository.UpdateAsync(answer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.NotifyAnswerUpdatedAsync(answer.QuestionId, answer.AnswerId, answer.Body, cancellationToken);

        return true;
    }
}

/// <summary>
/// Handler for DeleteAnswerCommand
/// </summary>
public class DeleteAnswerCommandHandler : IRequestHandler<DeleteAnswerCommand, bool>
{
    private readonly IAnswerRepository _answerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAnswerCommandHandler(IAnswerRepository answerRepository, IUnitOfWork unitOfWork)
    {
        _answerRepository = answerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteAnswerCommand request, CancellationToken cancellationToken)
    {
        var answer = await _answerRepository.GetByIdAsync(request.AnswerId, cancellationToken);

        if (answer == null || answer.UserId != request.UserId)
            return false;

        await _answerRepository.DeleteAsync(request.AnswerId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
    private readonly IQuestionEventDispatcher _questionDispatcher;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptAnswerCommandHandler(
        IAnswerRepository answerRepository,
        IQuestionRepository questionRepository,
        IDomainEventDispatcher eventDispatcher,
        IQuestionEventDispatcher questionDispatcher,
        IUnitOfWork unitOfWork)
    {
        _answerRepository = answerRepository;
        _questionRepository = questionRepository;
        _eventDispatcher = eventDispatcher;
        _questionDispatcher = questionDispatcher;
        _unitOfWork = unitOfWork;
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _questionDispatcher.NotifyAnswerAcceptedAsync(request.QuestionId, request.AnswerId, cancellationToken);

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
