using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Events;
using SocialTechsy.SocialNetwork.Shared.Constants;

namespace SocialTechsy.SocialNetwork.Application.EventHandlers;

public class VoteReputationHandler : INotificationHandler<DomainEventNotification<VoteCastEvent>>
{
    private readonly IUserRepository _userRepository;
    private readonly IReputationEventDispatcher _dispatcher;

    public VoteReputationHandler(
        IUserRepository userRepository,
        IReputationEventDispatcher dispatcher)
    {
        _userRepository = userRepository;
        _dispatcher = dispatcher;
    }

    public async Task Handle(DomainEventNotification<VoteCastEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var isQuestion = e.QuestionId.HasValue;

        if (e.IsDirectionChange)
        {
            var oldRep = e.WasPreviouslyUpvote
                ? (isQuestion ? ReputationPoints.QuestionUpvote : ReputationPoints.AnswerUpvote)
                : (isQuestion ? ReputationPoints.QuestionDownvote : ReputationPoints.AnswerDownvote);
            var newRep = e.IsUpvote
                ? (isQuestion ? ReputationPoints.QuestionUpvote : ReputationPoints.AnswerUpvote)
                : (isQuestion ? ReputationPoints.QuestionDownvote : ReputationPoints.AnswerDownvote);

            await _userRepository.UpdateReputationAsync(e.ContentAuthorId, -oldRep + newRep, cancellationToken);
            await NotifyReputationChangedAsync(e.ContentAuthorId, -oldRep + newRep, "Vote Changed", cancellationToken);

            if (!e.WasPreviouslyUpvote)
            {
                await _userRepository.UpdateReputationAsync(e.VoterId, -ReputationPoints.DownvoteCost, cancellationToken);
                await NotifyReputationChangedAsync(e.VoterId, -ReputationPoints.DownvoteCost, "Vote Reverted", cancellationToken);
            }
            else if (!e.IsUpvote)
            {
                await _userRepository.UpdateReputationAsync(e.VoterId, ReputationPoints.DownvoteCost, cancellationToken);
                await NotifyReputationChangedAsync(e.VoterId, ReputationPoints.DownvoteCost, "Voted Down", cancellationToken);
            }
        }
        else if (e.IsNewVote)
        {
            var repChange = e.IsUpvote
                ? (isQuestion ? ReputationPoints.QuestionUpvote : ReputationPoints.AnswerUpvote)
                : (isQuestion ? ReputationPoints.QuestionDownvote : ReputationPoints.AnswerDownvote);

            await _userRepository.UpdateReputationAsync(e.ContentAuthorId, repChange, cancellationToken);
            await NotifyReputationChangedAsync(e.ContentAuthorId, repChange, e.IsUpvote ? "Received Upvote" : "Received Downvote", cancellationToken);

            if (!e.IsUpvote)
            {
                await _userRepository.UpdateReputationAsync(e.VoterId, ReputationPoints.DownvoteCost, cancellationToken);
                await NotifyReputationChangedAsync(e.VoterId, ReputationPoints.DownvoteCost, "Voted Down", cancellationToken);
            }
        }
    }

    private async Task NotifyReputationChangedAsync(int userId, int delta, string reason, CancellationToken cancellationToken)
    {
        if (delta == 0) return;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user != null)
        {
            await _dispatcher.NotifyReputationChangedAsync(userId, user.ReputationPoints, delta, reason, cancellationToken);
        }
    }
}
