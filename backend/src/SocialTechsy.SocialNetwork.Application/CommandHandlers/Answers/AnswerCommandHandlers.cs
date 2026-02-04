using SocialTechsy.SocialNetwork.Application.Commands.Answers;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Answers;

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
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;

    public AcceptAnswerCommandHandler(
        IAnswerRepository answerRepository,
        IQuestionRepository questionRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository)
    {
        _answerRepository = answerRepository;
        _questionRepository = questionRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<AcceptAnswerResult> HandleAsync(AcceptAnswerCommand command, CancellationToken cancellationToken = default)
    {
        // Verify question belongs to user
        var question = await _questionRepository.GetByIdAsync(command.QuestionId, cancellationToken);
        if (question == null || question.UserId != command.UserId)
            return new AcceptAnswerResult { Success = false, Message = "Not authorized to accept answers for this question" };

        // Get the answer to check ownership
        var answer = await _answerRepository.GetByIdAsync(command.AnswerId, cancellationToken);
        if (answer == null)
            return new AcceptAnswerResult { Success = false, Message = "Answer not found" };

        // Check if already accepted
        if (answer.IsAccepted)
            return new AcceptAnswerResult { Success = true, Message = "Answer already accepted" };

        var success = await _answerRepository.AcceptAnswerAsync(command.AnswerId, command.QuestionId, cancellationToken);
        
        if (success)
        {
            // Award reputation to answer author (+15)
            await _userRepository.UpdateReputationAsync(
                answer.UserId, 
                CommandHandlers.Votes.ReputationPoints.AcceptedAnswerAuthor, 
                cancellationToken);
            
            // Award reputation to question owner (+2)
            await _userRepository.UpdateReputationAsync(
                question.UserId, 
                CommandHandlers.Votes.ReputationPoints.AcceptedAnswerOwner, 
                cancellationToken);

            // Create notification for answer author
            if (answer.UserId != question.UserId)
            {
                var questionOwner = await _userRepository.GetByIdAsync(question.UserId, cancellationToken);
                var notification = new Notification
                {
                    UserId = answer.UserId,
                    FromUserId = question.UserId,
                    Type = "AcceptedAnswer",
                    Message = $"{questionOwner?.DisplayName ?? questionOwner?.Username ?? "Someone"} accepted your answer to: {question.Title}",
                    Link = $"/questions/{question.QuestionId}",
                    CreatedDate = DateTime.UtcNow,
                    IsRead = false
                };
                await _notificationRepository.AddAsync(notification, cancellationToken);
                
                return new AcceptAnswerResult 
                { 
                    Success = true, 
                    Message = "Answer accepted successfully",
                    CreatedNotification = notification
                };
            }
        }
        
        return new AcceptAnswerResult { Success = success };
    }
}

/// <summary>
/// Result of accepting an answer
/// </summary>
public class AcceptAnswerResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Notification? CreatedNotification { get; set; }
}
