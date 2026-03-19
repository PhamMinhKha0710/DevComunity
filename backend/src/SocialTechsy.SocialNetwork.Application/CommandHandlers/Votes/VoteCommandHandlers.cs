using System.Text.Json;
using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Votes;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Enums;
using SocialTechsy.SocialNetwork.Domain.Events;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Votes;

public static class ReputationPoints
{
    public const int QuestionUpvote = 5;
    public const int QuestionDownvote = -2;
    public const int AnswerUpvote = 10;
    public const int AnswerDownvote = -2;
    public const int DownvoteCost = -1;
    public const int AcceptedAnswerAuthor = 15;
    public const int AcceptedAnswerOwner = 2;
    public const int AskQuestion = 2;
}

public class VoteQuestionCommandHandler : IRequestHandler<VoteQuestionCommand, VoteResult>
{
    private readonly IVoteRepository _voteRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILikeService? _likeService;
    private readonly IActivityLogService? _activityLog;
    private readonly IOutboxRepository? _outboxRepository;

    public VoteQuestionCommandHandler(
        IVoteRepository voteRepository,
        IQuestionRepository questionRepository,
        IUserRepository userRepository,
        IDomainEventDispatcher eventDispatcher,
        IUnitOfWork unitOfWork,
        ILikeService? likeService = null,
        IActivityLogService? activityLog = null,
        IOutboxRepository? outboxRepository = null)
    {
        _voteRepository = voteRepository;
        _questionRepository = questionRepository;
        _userRepository = userRepository;
        _eventDispatcher = eventDispatcher;
        _unitOfWork = unitOfWork;
        _likeService = likeService;
        _activityLog = activityLog;
        _outboxRepository = outboxRepository;
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
            existingVote.ChangeDirection(isUpvote);
            await _voteRepository.UpdateAsync(existingVote, cancellationToken);
        }
        else
        {
            var vote = Vote.Create(request.UserId, isUpvote, request.QuestionId, null);
            await _voteRepository.AddAsync(vote, cancellationToken);
        }

        long likeCount = 0;
        if (_likeService != null && isUpvote)
        {
            likeCount = await _likeService.LikeAsync("question", request.QuestionId, request.UserId);
        }
        else if (_likeService != null && !isUpvote)
        {
            likeCount = await _likeService.UnlikeAsync("question", request.QuestionId, request.UserId);
        }

        if (_activityLog != null && isUpvote)
            _ = _activityLog.LogLikeAsync("question", request.QuestionId, request.UserId);

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

        if (_outboxRepository != null && isUpvote && (isNewVote || isDirectionChange))
        {
            var likeEvent = new LikeEvent
            {
                TargetType = "question",
                TargetId = request.QuestionId,
                LikedByUserId = request.UserId,
                LikedByDisplayName = voter?.DisplayName ?? voter?.Username ?? "Someone",
                ContentAuthorId = question.UserId,
                ContentTitle = question.Title,
                QuestionId = request.QuestionId,
                LikeCount = likeCount
            };
            await _outboxRepository.AddAsync(new OutboxMessage
            {
                EventType = "like.question",
                PayloadJson = JsonSerializer.Serialize(likeEvent),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var score = _likeService != null
            ? (int)likeCount
            : await _voteRepository.GetQuestionScoreAsync(request.QuestionId, cancellationToken);

        return new VoteResult
        {
            Success = true,
            Score = score,
            UserVote = request.VoteType
        };
    }
}

public class VoteAnswerCommandHandler : IRequestHandler<VoteAnswerCommand, VoteResult>
{
    private readonly IVoteRepository _voteRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILikeService? _likeService;
    private readonly IActivityLogService? _activityLog;
    private readonly IOutboxRepository? _outboxRepository;

    public VoteAnswerCommandHandler(
        IVoteRepository voteRepository,
        IAnswerRepository answerRepository,
        IUserRepository userRepository,
        IDomainEventDispatcher eventDispatcher,
        IUnitOfWork unitOfWork,
        ILikeService? likeService = null,
        IActivityLogService? activityLog = null,
        IOutboxRepository? outboxRepository = null)
    {
        _voteRepository = voteRepository;
        _answerRepository = answerRepository;
        _userRepository = userRepository;
        _eventDispatcher = eventDispatcher;
        _unitOfWork = unitOfWork;
        _likeService = likeService;
        _activityLog = activityLog;
        _outboxRepository = outboxRepository;
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
            existingVote.ChangeDirection(isUpvote);
            await _voteRepository.UpdateAsync(existingVote, cancellationToken);
        }
        else
        {
            var vote = Vote.Create(request.UserId, isUpvote, null, request.AnswerId);
            await _voteRepository.AddAsync(vote, cancellationToken);
        }

        long likeCount = 0;
        if (_likeService != null && isUpvote)
        {
            likeCount = await _likeService.LikeAsync("answer", request.AnswerId, request.UserId);
        }
        else if (_likeService != null && !isUpvote)
        {
            likeCount = await _likeService.UnlikeAsync("answer", request.AnswerId, request.UserId);
        }

        if (_activityLog != null && isUpvote)
            _ = _activityLog.LogLikeAsync("answer", request.AnswerId, request.UserId);

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

        if (_outboxRepository != null && isUpvote && (isNewVote || isDirectionChange))
        {
            var likeEvent = new LikeEvent
            {
                TargetType = "answer",
                TargetId = request.AnswerId,
                LikedByUserId = request.UserId,
                LikedByDisplayName = voter?.DisplayName ?? voter?.Username ?? "Someone",
                ContentAuthorId = answer.UserId,
                QuestionId = answer.QuestionId,
                LikeCount = likeCount
            };
            await _outboxRepository.AddAsync(new OutboxMessage
            {
                EventType = "like.answer",
                PayloadJson = JsonSerializer.Serialize(likeEvent),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var score = _likeService != null
            ? (int)likeCount
            : await _voteRepository.GetAnswerScoreAsync(request.AnswerId, cancellationToken);

        return new VoteResult
        {
            Success = true,
            Score = score,
            UserVote = request.VoteType,
            QuestionId = answer.QuestionId
        };
    }
}

public class RemoveVoteCommandHandler : IRequestHandler<RemoveVoteCommand, VoteResult>
{
    private readonly IVoteRepository _voteRepository;
    private readonly IAnswerRepository? _answerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILikeService? _likeService;
    private readonly IActivityLogService? _activityLog;

    public RemoveVoteCommandHandler(
        IVoteRepository voteRepository,
        IAnswerRepository? answerRepository = null,
        IUnitOfWork unitOfWork = null!,
        ILikeService? likeService = null,
        IActivityLogService? activityLog = null)
    {
        _voteRepository = voteRepository;
        _answerRepository = answerRepository;
        _unitOfWork = unitOfWork;
        _likeService = likeService;
        _activityLog = activityLog;
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
                if (_likeService != null)
                    await _likeService.UnlikeAsync("question", request.QuestionId.Value, request.UserId);
                if (_activityLog != null)
                    _ = _activityLog.LogUnlikeAsync("question", request.QuestionId.Value, request.UserId);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var score = _likeService != null
                ? (int)await _likeService.GetLikeCountAsync("question", request.QuestionId.Value)
                : await _voteRepository.GetQuestionScoreAsync(request.QuestionId.Value, cancellationToken);
            return new VoteResult { Success = true, Score = score };
        }
        else if (request.AnswerId.HasValue)
        {
            var vote = await _voteRepository.GetUserVoteOnAnswerAsync(
                request.UserId, request.AnswerId.Value, cancellationToken);
            if (vote != null)
            {
                await _voteRepository.DeleteAsync(vote.VoteId, cancellationToken);
                if (_likeService != null)
                    await _likeService.UnlikeAsync("answer", request.AnswerId.Value, request.UserId);
                if (_activityLog != null)
                    _ = _activityLog.LogUnlikeAsync("answer", request.AnswerId.Value, request.UserId);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var score = _likeService != null
                ? (int)await _likeService.GetLikeCountAsync("answer", request.AnswerId.Value)
                : await _voteRepository.GetAnswerScoreAsync(request.AnswerId.Value, cancellationToken);
            int? questionId = null;
            if (_answerRepository != null)
            {
                var answer = await _answerRepository.GetByIdAsync(request.AnswerId.Value, cancellationToken);
                questionId = answer?.QuestionId;
            }
            return new VoteResult { Success = true, Score = score, QuestionId = questionId };
        }

        return new VoteResult { Success = false, Message = "Invalid vote target" };
    }
}

public class VoteResult
{
    public bool Success { get; set; }
    public int Score { get; set; }
    public VoteType? UserVote { get; set; }
    public string? Message { get; set; }
    /// <summary>Set when voting on an answer, so API can emit SignalR to the question room.</summary>
    public int? QuestionId { get; set; }
}
