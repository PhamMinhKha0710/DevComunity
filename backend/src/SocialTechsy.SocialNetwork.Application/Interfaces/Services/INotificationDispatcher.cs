using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

/// <summary>
/// Handles SignalR push for generic notifications. Implemented in the API layer
/// where SignalR hub contexts are available.
/// </summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(Notification notification, CancellationToken cancellationToken = default);
}
