using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Events;
using SocialTechsy.SocialNetwork.Shared.Constants;

namespace SocialTechsy.SocialNetwork.Application.EventHandlers;

public class AnswerAcceptedReputationHandler : INotificationHandler<DomainEventNotification<AnswerAcceptedEvent>>
{
    private readonly IUserRepository _userRepository;

    public AnswerAcceptedReputationHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(DomainEventNotification<AnswerAcceptedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        await _userRepository.UpdateReputationAsync(
            e.AnswerAuthorId, ReputationPoints.AcceptedAnswerAuthor, cancellationToken);
        await _userRepository.UpdateReputationAsync(
            e.QuestionOwnerId, ReputationPoints.AcceptedAnswerOwner, cancellationToken);
    }
}
