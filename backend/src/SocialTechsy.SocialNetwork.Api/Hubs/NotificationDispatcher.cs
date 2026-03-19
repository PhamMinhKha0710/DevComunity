using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Api.Hubs;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(IHubContext<NotificationHub> hubContext, ILogger<NotificationDispatcher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task DispatchAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{notification.UserId}")
                .SendAsync("ReceiveNotification", notification, cancellationToken);
            _logger.LogInformation("SignalR generic notification pushed to User {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR generic notification to User {UserId}", notification.UserId);
        }
    }
}
