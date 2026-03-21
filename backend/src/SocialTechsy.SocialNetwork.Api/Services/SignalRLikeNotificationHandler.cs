using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Api.Hubs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Api.Services;

public class SignalRLikeNotificationHandler : ILikeNotificationHandler
{
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly IHubContext<QuestionHub> _questionHub;
    private readonly ILogger<SignalRLikeNotificationHandler> _logger;

    public SignalRLikeNotificationHandler(
        IHubContext<NotificationHub> notificationHub,
        IHubContext<QuestionHub> questionHub,
        ILogger<SignalRLikeNotificationHandler> logger)
    {
        _notificationHub = notificationHub;
        _questionHub = questionHub;
        _logger = logger;
    }

    public async Task HandleAsync(LikeEvent likeEvent, Notification notification)
    {
        try
        {
            await _notificationHub.Clients.Group($"user_{likeEvent.ContentAuthorId}")
                .SendAsync("ReceiveNotification", new
                {
                    notification.NotificationId,
                    notification.Type,
                    notification.Message,
                    notification.Link,
                    notification.CreatedDate,
                    notification.IsRead,
                    notification.FromUserId
                });

            var questionId = likeEvent.QuestionId ?? likeEvent.TargetId;
            await _questionHub.Clients.Group($"question_{questionId}")
                .SendAsync("VoteChanged", new
                {
                    likeEvent.TargetType,
                    likeEvent.TargetId,
                    likeEvent.LikeCount,
                    likeEvent.LikedByUserId
                });

            _logger.LogInformation(
                "SignalR: Sent ReceiveNotification to User {UserId} and VoteChanged to question {QuestionId}",
                likeEvent.ContentAuthorId, questionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push SignalR notification for like event");
        }
    }
}
