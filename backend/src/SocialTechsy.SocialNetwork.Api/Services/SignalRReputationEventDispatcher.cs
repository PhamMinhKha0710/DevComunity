using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Api.Hubs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Api.Services;

public class SignalRReputationEventDispatcher : IReputationEventDispatcher
{
    private readonly IHubContext<NotificationHub> _notificationHub;

    public SignalRReputationEventDispatcher(IHubContext<NotificationHub> notificationHub)
    {
        _notificationHub = notificationHub;
    }

    public async Task NotifyReputationChangedAsync(int userId, int newScore, int delta, string reason, CancellationToken cancellationToken = default)
    {
        await _notificationHub.Clients.Group($"user_{userId}").SendAsync("ReputationChanged", new { NewScore = newScore, Delta = delta, Reason = reason }, cancellationToken);
    }
}
