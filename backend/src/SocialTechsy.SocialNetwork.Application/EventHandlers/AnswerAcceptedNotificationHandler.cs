using System.Text.Json;
using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Events;

namespace SocialTechsy.SocialNetwork.Application.EventHandlers;

/// <summary>
/// Writes an outbox message for the AnswerAccepted notification instead of creating
/// the SQL notification synchronously. The notification will be created by a consumer
/// processing the outbox event, decoupling it from the request path.
/// </summary>
public class AnswerAcceptedNotificationHandler : INotificationHandler<DomainEventNotification<AnswerAcceptedEvent>>
{
    private readonly IOutboxRepository? _outboxRepository;
    private readonly INotificationRepository _notificationRepository;

    public AnswerAcceptedNotificationHandler(
        INotificationRepository notificationRepository,
        IOutboxRepository? outboxRepository = null)
    {
        _notificationRepository = notificationRepository;
        _outboxRepository = outboxRepository;
    }

    public async Task Handle(DomainEventNotification<AnswerAcceptedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        if (e.AnswerAuthorId == e.QuestionOwnerId)
            return;

        if (_outboxRepository != null)
        {
            await _outboxRepository.AddAsync(new OutboxMessage
            {
                EventType = "notification.answer_accepted",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    e.AnswerAuthorId,
                    e.QuestionOwnerId,
                    e.QuestionId,
                    e.QuestionTitle
                }),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
            return;
        }

        // Fallback: create notification directly if outbox is unavailable
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
