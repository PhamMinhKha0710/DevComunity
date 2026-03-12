using MediatR;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Votes;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Events;

namespace SocialTechsy.SocialNetwork.Application.EventHandlers;

public class VoteReputationHandler : INotificationHandler<DomainEventNotification<VoteCastEvent>>
{
    private readonly IUserRepository _userRepository;

    public VoteReputationHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
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

            if (!e.WasPreviouslyUpvote)
                await _userRepository.UpdateReputationAsync(e.VoterId, -ReputationPoints.DownvoteCost, cancellationToken);
            else if (!e.IsUpvote)
                await _userRepository.UpdateReputationAsync(e.VoterId, ReputationPoints.DownvoteCost, cancellationToken);
        }
        else if (e.IsNewVote)
        {
            var repChange = e.IsUpvote
                ? (isQuestion ? ReputationPoints.QuestionUpvote : ReputationPoints.AnswerUpvote)
                : (isQuestion ? ReputationPoints.QuestionDownvote : ReputationPoints.AnswerDownvote);

            await _userRepository.UpdateReputationAsync(e.ContentAuthorId, repChange, cancellationToken);

            if (!e.IsUpvote)
                await _userRepository.UpdateReputationAsync(e.VoterId, ReputationPoints.DownvoteCost, cancellationToken);
        }
    }
}
