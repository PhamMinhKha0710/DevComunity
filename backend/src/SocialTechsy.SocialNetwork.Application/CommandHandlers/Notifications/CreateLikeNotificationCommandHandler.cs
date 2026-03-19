using MediatR;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Commands.Notifications;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Notifications;

public class CreateLikeNotificationCommandHandler : IRequestHandler<CreateLikeNotificationCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILikeService _likeService;
    private readonly ILikeNotificationHandler _signalRHandler;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateLikeNotificationCommandHandler> _logger;

    public CreateLikeNotificationCommandHandler(
        INotificationRepository notificationRepository,
        ILikeService likeService,
        ILikeNotificationHandler signalRHandler,
        IUnitOfWork unitOfWork,
        ILogger<CreateLikeNotificationCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _likeService = likeService;
        _signalRHandler = signalRHandler;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CreateLikeNotificationCommand request, CancellationToken cancellationToken)
    {
        if (request.LikedByUserId == request.ContentAuthorId)
            return;

        await _likeService.LikeAsync(request.TargetType, request.TargetId, request.LikedByUserId);

        var link = request.TargetType == "question"
            ? $"/questions/{request.TargetId}"
            : $"/questions/{request.QuestionId}#answer-{request.TargetId}";

        var message = request.TargetType == "question"
            ? $"{request.LikedByDisplayName} liked your question: {request.ContentTitle}"
            : $"{request.LikedByDisplayName} liked your answer";

        var notification = new Notification
        {
            UserId = request.ContentAuthorId,
            FromUserId = request.LikedByUserId,
            Type = "Like",
            Message = message,
            Link = link,
            CreatedDate = DateTime.UtcNow,
            IsRead = false
        };

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _signalRHandler.HandleAsync(new LikeEvent
        {
            EventId = request.EventId,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            LikedByUserId = request.LikedByUserId,
            LikedByDisplayName = request.LikedByDisplayName,
            ContentAuthorId = request.ContentAuthorId,
            ContentTitle = request.ContentTitle,
            QuestionId = request.QuestionId,
            LikeCount = request.LikeCount
        }, notification);

        _logger.LogInformation(
            "Processed like notification: {TargetType}:{TargetId} by user {UserId}",
            request.TargetType, request.TargetId, request.LikedByUserId);
    }
}
