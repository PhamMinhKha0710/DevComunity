using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Events;

namespace SocialTechsy.SocialNetwork.Application.EventHandlers;

public class AnswerAcceptedNotificationHandler : INotificationHandler<DomainEventNotification<AnswerAcceptedEvent>>
{
    private readonly INotificationRepository _notificationRepository;

    public AnswerAcceptedNotificationHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(DomainEventNotification<AnswerAcceptedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        if (e.AnswerAuthorId == e.QuestionOwnerId)
            return;

        var notificationEntity = new Notification
        {
            UserId = e.AnswerAuthorId,
            FromUserId = e.QuestionOwnerId,
            Type = "AnswerAccepted",
            Message = $"Your answer was accepted on: {e.QuestionTitle}",
            Link = $"/questions/{e.QuestionId}",
            CreatedDate = DateTime.UtcNow,
            IsRead = false
        };

        await _notificationRepository.AddAsync(notificationEntity, cancellationToken);
    }
}
