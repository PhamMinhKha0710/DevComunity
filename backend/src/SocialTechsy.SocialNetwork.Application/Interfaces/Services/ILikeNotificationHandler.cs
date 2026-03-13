using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

/// <summary>
/// Handles SignalR push for like events. Implemented in the API layer
/// where SignalR hub contexts are available.
/// </summary>
public interface ILikeNotificationHandler
{
    Task HandleAsync(LikeEvent likeEvent, Notification notification);
}
