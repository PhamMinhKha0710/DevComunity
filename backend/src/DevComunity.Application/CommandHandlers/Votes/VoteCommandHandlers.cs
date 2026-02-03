using DevComunity.Application.Commands.Votes;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;

namespace DevComunity.Application.CommandHandlers.Votes;

// Reputation constants
public static class ReputationPoints
{
    public const int QuestionUpvote = 5;
    public const int QuestionDownvote = -2;
    public const int AnswerUpvote = 10;
    public const int AnswerDownvote = -2;
    public const int DownvoteCost = -1; // Voter penalty for downvoting
    public const int AcceptedAnswerAuthor = 15; // Bonus for answer author when accepted
    public const int AcceptedAnswerOwner = 2;   // Bonus for question owner for accepting
    public const int AskQuestion = 2; // Bonus for asking a question
}


/// <summary>
/// Handler for voting on a question
/// </summary>
public class VoteQuestionCommandHandler
{
    private readonly IVoteRepository _voteRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;

    public VoteQuestionCommandHandler(
        IVoteRepository voteRepository,
        IQuestionRepository questionRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository)
    {
        _voteRepository = voteRepository;
        _questionRepository = questionRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<VoteResult> HandleAsync(VoteQuestionCommand command, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(command.QuestionId, cancellationToken);
        if (question == null)
            return new VoteResult { Success = false, Message = "Question not found" };

        // Prevent self-voting
        if (question.UserId == command.UserId)
            return new VoteResult { Success = false, Message = "Cannot vote on your own question" };

        var existingVote = await _voteRepository.GetUserVoteOnQuestionAsync(
            command.UserId, command.QuestionId, cancellationToken);

        var isUpvote = command.VoteType.ToLower() == "up";

        if (existingVote != null)
        {
            // If changing vote direction, adjust reputation
            if (existingVote.IsUpvote != isUpvote)
            {
                var oldRepChange = existingVote.IsUpvote ? ReputationPoints.QuestionUpvote : ReputationPoints.QuestionDownvote;
                var newRepChange = isUpvote ? ReputationPoints.QuestionUpvote : ReputationPoints.QuestionDownvote;
                
                // Reverse old reputation and apply new
                await _userRepository.UpdateReputationAsync(question.UserId, -oldRepChange + newRepChange, cancellationToken);
                
                // Adjust voter's downvote penalty
                if (!existingVote.IsUpvote) // Was downvote, now upvote - refund penalty
                    await _userRepository.UpdateReputationAsync(command.UserId, -ReputationPoints.DownvoteCost, cancellationToken);
                else if (!isUpvote) // Was upvote, now downvote - apply penalty
                    await _userRepository.UpdateReputationAsync(command.UserId, ReputationPoints.DownvoteCost, cancellationToken);
            }
            
            existingVote.IsUpvote = isUpvote;
            await _voteRepository.UpdateAsync(existingVote, cancellationToken);
        }
        else
        {
            // New vote - update author reputation
            var repChange = isUpvote ? ReputationPoints.QuestionUpvote : ReputationPoints.QuestionDownvote;
            await _userRepository.UpdateReputationAsync(question.UserId, repChange, cancellationToken);
            
            // Downvote costs the voter
            if (!isUpvote)
                await _userRepository.UpdateReputationAsync(command.UserId, ReputationPoints.DownvoteCost, cancellationToken);

            var vote = new Vote
            {
                UserId = command.UserId,
                QuestionId = command.QuestionId,
                IsUpvote = isUpvote,
                CreatedDate = DateTime.UtcNow
            };
            await _voteRepository.AddAsync(vote, cancellationToken);
        }

        var score = await _voteRepository.GetQuestionScoreAsync(command.QuestionId, cancellationToken);

        // Check if we need to send notification (Only on Upvote and not self-vote)
        Notification? notification = null;
        if (isUpvote && command.UserId != question.UserId)
        {
             // Check if already notified recently? For now, just simplest logic: Notify on every new upvote.
             // But usually we should avoid duplicate notifications.
             // Ideally check if notification exists. But let's keep it simple for MVP.
             // If vote was changed from Down to Up, or New Upvote.
             bool shouldNotify = (existingVote == null) || (existingVote != null && !existingVote.IsUpvote);
             
             if (shouldNotify)
             {
                 var voter = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
                 notification = new Notification
                 {
                     UserId = question.UserId,
                     FromUserId = command.UserId,
                     Type = "Upvote",
                     Message = $"{voter?.DisplayName ?? voter?.Username ?? "Someone"} upvoted your question: {question.Title}",
                     Link = $"/questions/{question.QuestionId}",
                     CreatedDate = DateTime.UtcNow,
                     IsRead = false
                 };
                 await _notificationRepository.AddAsync(notification, cancellationToken);
             }
        }

        return new VoteResult
        {
            Success = true,
            Score = score,
            UserVote = command.VoteType,
            CreatedNotification = notification
        };
    }
}

/// <summary>
/// Handler for voting on an answer
/// </summary>
public class VoteAnswerCommandHandler
{
    private readonly IVoteRepository _voteRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;

    public VoteAnswerCommandHandler(
        IVoteRepository voteRepository,
        IAnswerRepository answerRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository)
    {
        _voteRepository = voteRepository;
        _answerRepository = answerRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<VoteResult> HandleAsync(VoteAnswerCommand command, CancellationToken cancellationToken)
    {
        var answer = await _answerRepository.GetByIdAsync(command.AnswerId, cancellationToken);
        if (answer == null)
            return new VoteResult { Success = false, Message = "Answer not found" };

        // Prevent self-voting
        if (answer.UserId == command.UserId)
            return new VoteResult { Success = false, Message = "Cannot vote on your own answer" };

        var existingVote = await _voteRepository.GetUserVoteOnAnswerAsync(
            command.UserId, command.AnswerId, cancellationToken);

        var isUpvote = command.VoteType.ToLower() == "up";

        if (existingVote != null)
        {
            // If changing vote direction, adjust reputation
            if (existingVote.IsUpvote != isUpvote)
            {
                var oldRepChange = existingVote.IsUpvote ? ReputationPoints.AnswerUpvote : ReputationPoints.AnswerDownvote;
                var newRepChange = isUpvote ? ReputationPoints.AnswerUpvote : ReputationPoints.AnswerDownvote;
                
                await _userRepository.UpdateReputationAsync(answer.UserId, -oldRepChange + newRepChange, cancellationToken);
                
                if (!existingVote.IsUpvote)
                    await _userRepository.UpdateReputationAsync(command.UserId, -ReputationPoints.DownvoteCost, cancellationToken);
                else if (!isUpvote)
                    await _userRepository.UpdateReputationAsync(command.UserId, ReputationPoints.DownvoteCost, cancellationToken);
            }
            
            existingVote.IsUpvote = isUpvote;
            await _voteRepository.UpdateAsync(existingVote, cancellationToken);
        }
        else
        {
            var repChange = isUpvote ? ReputationPoints.AnswerUpvote : ReputationPoints.AnswerDownvote;
            await _userRepository.UpdateReputationAsync(answer.UserId, repChange, cancellationToken);
            
            if (!isUpvote)
                await _userRepository.UpdateReputationAsync(command.UserId, ReputationPoints.DownvoteCost, cancellationToken);

            var vote = new Vote
            {
                UserId = command.UserId,
                AnswerId = command.AnswerId,
                IsUpvote = isUpvote,
                CreatedDate = DateTime.UtcNow
            };
            await _voteRepository.AddAsync(vote, cancellationToken);
        }

        var score = await _voteRepository.GetAnswerScoreAsync(command.AnswerId, cancellationToken);

         // Notification Logic for Answer Upvote
        Notification? notification = null;
        if (isUpvote && command.UserId != answer.UserId)
        {
             bool shouldNotify = (existingVote == null) || (existingVote != null && !existingVote.IsUpvote);
             
             if (shouldNotify)
             {
                 var voter = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
                 notification = new Notification
                 {
                     UserId = answer.UserId,
                     FromUserId = command.UserId,
                     Type = "Upvote",
                     Message = $"{voter?.DisplayName ?? voter?.Username ?? "Someone"} upvoted your answer",
                     Link = $"/questions/{answer.QuestionId}", // Link to question, maybe anchor to answer?
                     CreatedDate = DateTime.UtcNow,
                     IsRead = false
                 };
                 await _notificationRepository.AddAsync(notification, cancellationToken);
             }
        }

        return new VoteResult
        {
            Success = true,
            Score = score,
            UserVote = command.VoteType,
            CreatedNotification = notification
        };
    }
}

/// <summary>
/// Handler for removing a vote
/// </summary>
public class RemoveVoteCommandHandler
{
    private readonly IVoteRepository _voteRepository;

    public RemoveVoteCommandHandler(IVoteRepository voteRepository)
    {
        _voteRepository = voteRepository;
    }

    public async Task<VoteResult> HandleAsync(RemoveVoteCommand command, CancellationToken cancellationToken)
    {
        if (command.QuestionId.HasValue)
        {
            var vote = await _voteRepository.GetUserVoteOnQuestionAsync(
                command.UserId, command.QuestionId.Value, cancellationToken);
            if (vote != null)
            {
                await _voteRepository.DeleteAsync(vote.VoteId, cancellationToken);
            }
            var score = await _voteRepository.GetQuestionScoreAsync(command.QuestionId.Value, cancellationToken);
            return new VoteResult { Success = true, Score = score };
        }
        else if (command.AnswerId.HasValue)
        {
            var vote = await _voteRepository.GetUserVoteOnAnswerAsync(
                command.UserId, command.AnswerId.Value, cancellationToken);
            if (vote != null)
            {
                await _voteRepository.DeleteAsync(vote.VoteId, cancellationToken);
            }
            var score = await _voteRepository.GetAnswerScoreAsync(command.AnswerId.Value, cancellationToken);
            return new VoteResult { Success = true, Score = score };
        }

        return new VoteResult { Success = false, Message = "Invalid vote target" };
    }
}

/// <summary>
/// Result of a vote operation
/// </summary>
public class VoteResult
{
    public bool Success { get; set; }
    public int Score { get; set; }
    public string? UserVote { get; set; }
    public string? Message { get; set; }
    public Notification? CreatedNotification { get; set; }
}

