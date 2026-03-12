using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Votes;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Enums;
using SocialTechsy.SocialNetwork.Domain.Events;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Votes;

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
public class VoteQuestionCommandHandler : IRequestHandler<VoteQuestionCommand, VoteResult>
{
    private readonly IVoteRepository _voteRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public VoteQuestionCommandHandler(
        IVoteRepository voteRepository,
        IQuestionRepository questionRepository,
        IUserRepository userRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _voteRepository = voteRepository;
        _questionRepository = questionRepository;
        _userRepository = userRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<VoteResult> Handle(VoteQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);
        if (question == null)
            return new VoteResult { Success = false, Message = "Question not found" };

        if (question.UserId == request.UserId)
            return new VoteResult { Success = false, Message = "Cannot vote on your own question" };

        var existingVote = await _voteRepository.GetUserVoteOnQuestionAsync(
            request.UserId, request.QuestionId, cancellationToken);

        var isUpvote = request.VoteType == VoteType.Up;
        var wasPreviouslyUpvote = existingVote?.IsUpvote ?? false;
        var isDirectionChange = existingVote != null && existingVote.IsUpvote != isUpvote;
        var isNewVote = existingVote == null;

        if (existingVote != null)
        {
            existingVote.IsUpvote = isUpvote;
            await _voteRepository.UpdateAsync(existingVote, cancellationToken);
        }
        else
        {
            var vote = new Vote
            {
                UserId = request.UserId,
                QuestionId = request.QuestionId,
                IsUpvote = isUpvote,
                CreatedDate = DateTime.UtcNow
            };
            await _voteRepository.AddAsync(vote, cancellationToken);
        }

        var voter = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        await _eventDispatcher.DispatchAsync(new VoteCastEvent
        {
            VoterId = request.UserId,
            ContentAuthorId = question.UserId,
            QuestionId = request.QuestionId,
            IsUpvote = isUpvote,
            IsNewVote = isNewVote,
            IsDirectionChange = isDirectionChange,
            WasPreviouslyUpvote = wasPreviouslyUpvote,
            ContentTitle = question.Title,
            VoterDisplayName = voter?.DisplayName ?? voter?.Username ?? "Someone"
        }, cancellationToken);

        var score = await _voteRepository.GetQuestionScoreAsync(request.QuestionId, cancellationToken);

        return new VoteResult
        {
            Success = true,
            Score = score,
            UserVote = request.VoteType
        };
    }
}

/// <summary>
/// Handler for voting on an answer
/// </summary>
public class VoteAnswerCommandHandler : IRequestHandler<VoteAnswerCommand, VoteResult>
{
    private readonly IVoteRepository _voteRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public VoteAnswerCommandHandler(
        IVoteRepository voteRepository,
        IAnswerRepository answerRepository,
        IUserRepository userRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _voteRepository = voteRepository;
        _answerRepository = answerRepository;
        _userRepository = userRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<VoteResult> Handle(VoteAnswerCommand request, CancellationToken cancellationToken)
    {
        var answer = await _answerRepository.GetByIdAsync(request.AnswerId, cancellationToken);
        if (answer == null)
            return new VoteResult { Success = false, Message = "Answer not found" };

        if (answer.UserId == request.UserId)
            return new VoteResult { Success = false, Message = "Cannot vote on your own answer" };

        var existingVote = await _voteRepository.GetUserVoteOnAnswerAsync(
            request.UserId, request.AnswerId, cancellationToken);

        var isUpvote = request.VoteType == VoteType.Up;
        var wasPreviouslyUpvote = existingVote?.IsUpvote ?? false;
        var isDirectionChange = existingVote != null && existingVote.IsUpvote != isUpvote;
        var isNewVote = existingVote == null;

        if (existingVote != null)
        {
            existingVote.IsUpvote = isUpvote;
            await _voteRepository.UpdateAsync(existingVote, cancellationToken);
        }
        else
        {
            var vote = new Vote
            {
                UserId = request.UserId,
                AnswerId = request.AnswerId,
                IsUpvote = isUpvote,
                CreatedDate = DateTime.UtcNow
            };
            await _voteRepository.AddAsync(vote, cancellationToken);
        }

        var voter = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        await _eventDispatcher.DispatchAsync(new VoteCastEvent
        {
            VoterId = request.UserId,
            ContentAuthorId = answer.UserId,
            AnswerId = request.AnswerId,
            IsUpvote = isUpvote,
            IsNewVote = isNewVote,
            IsDirectionChange = isDirectionChange,
            WasPreviouslyUpvote = wasPreviouslyUpvote,
            VoterDisplayName = voter?.DisplayName ?? voter?.Username ?? "Someone"
        }, cancellationToken);

        var score = await _voteRepository.GetAnswerScoreAsync(request.AnswerId, cancellationToken);

        return new VoteResult
        {
            Success = true,
            Score = score,
            UserVote = request.VoteType
        };
    }
}

/// <summary>
/// Handler for removing a vote
/// </summary>
public class RemoveVoteCommandHandler : IRequestHandler<RemoveVoteCommand, VoteResult>
{
    private readonly IVoteRepository _voteRepository;

    public RemoveVoteCommandHandler(IVoteRepository voteRepository)
    {
        _voteRepository = voteRepository;
    }

    public async Task<VoteResult> Handle(RemoveVoteCommand request, CancellationToken cancellationToken)
    {
        if (request.QuestionId.HasValue)
        {
            var vote = await _voteRepository.GetUserVoteOnQuestionAsync(
                request.UserId, request.QuestionId.Value, cancellationToken);
            if (vote != null)
            {
                await _voteRepository.DeleteAsync(vote.VoteId, cancellationToken);
            }
            var score = await _voteRepository.GetQuestionScoreAsync(request.QuestionId.Value, cancellationToken);
            return new VoteResult { Success = true, Score = score };
        }
        else if (request.AnswerId.HasValue)
        {
            var vote = await _voteRepository.GetUserVoteOnAnswerAsync(
                request.UserId, request.AnswerId.Value, cancellationToken);
            if (vote != null)
            {
                await _voteRepository.DeleteAsync(vote.VoteId, cancellationToken);
            }
            var score = await _voteRepository.GetAnswerScoreAsync(request.AnswerId.Value, cancellationToken);
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
    public VoteType? UserVote { get; set; }
    public string? Message { get; set; }
}
