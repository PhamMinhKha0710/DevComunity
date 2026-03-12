using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Events;

namespace SocialTechsy.SocialNetwork.Application.EventHandlers;

public class VoteNotificationHandler : INotificationHandler<DomainEventNotification<VoteCastEvent>>
{
    private readonly INotificationRepository _notificationRepository;

    public VoteNotificationHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(DomainEventNotification<VoteCastEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        if (!e.IsUpvote || e.VoterId == e.ContentAuthorId)
            return;

        bool shouldNotify = e.IsNewVote || (e.IsDirectionChange && !e.WasPreviouslyUpvote);
        if (!shouldNotify)
            return;

        var link = e.QuestionId.HasValue
            ? $"/questions/{e.QuestionId}"
            : $"/questions/{e.AnswerId}";

        var targetMessage = e.QuestionId.HasValue
            ? $"{e.VoterDisplayName} upvoted your question: {e.ContentTitle}"
            : $"{e.VoterDisplayName} upvoted your answer";

        var notificationEntity = new Notification
        {
            UserId = e.ContentAuthorId,
            FromUserId = e.VoterId,
            Type = "Upvote",
            Message = targetMessage,
            Link = link,
            CreatedDate = DateTime.UtcNow,
            IsRead = false
        };

        await _notificationRepository.AddAsync(notificationEntity, cancellationToken);
    }
}
