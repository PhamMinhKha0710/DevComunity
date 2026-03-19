using MediatR;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Commands.Notifications;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Notifications;

public class CreateChatNotificationCommandHandler : IRequestHandler<CreateChatNotificationCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IChatPushHandler _chatPushHandler;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateChatNotificationCommandHandler> _logger;

    public CreateChatNotificationCommandHandler(
        INotificationRepository notificationRepository,
        IChatPushHandler chatPushHandler,
        IUnitOfWork unitOfWork,
        ILogger<CreateChatNotificationCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _chatPushHandler = chatPushHandler;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CreateChatNotificationCommand request, CancellationToken cancellationToken)
    {
        foreach (var recipientId in request.RecipientIds)
        {
            var notification = new Notification
            {
                UserId = recipientId,
                FromUserId = request.SenderId,
                Type = "chat_message",
                Message = $"New message from {request.SenderUsername}",
                Link = $"/chat?conversation={request.ConversationId}",
                IsRead = false,
                CreatedDate = request.Timestamp
            };
            await _notificationRepository.AddAsync(notification, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var pushEvent = new ChatPushEvent
        {
            EventId = Guid.NewGuid().ToString(),
            ConversationId = request.ConversationId,
            MessageId = request.MessageId,
            SenderId = request.SenderId,
            SenderUsername = request.SenderUsername,
            NotificationPreview = $"New message from {request.SenderUsername}",
            RecipientUserIds = request.RecipientIds
        };

        await _chatPushHandler.PushNewMessageNotificationAsync(pushEvent);

        _logger.LogDebug(
            "Processed chat notification for conversation {ConversationId}",
            request.ConversationId);
    }
}
