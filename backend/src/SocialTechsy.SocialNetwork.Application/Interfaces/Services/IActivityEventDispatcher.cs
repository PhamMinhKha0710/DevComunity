using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

/// <summary>
/// Dispatches activity events (new posts, new group posts) via SignalR.
/// Implemented in the API layer where IHubContext is available.
/// </summary>
public interface IActivityEventDispatcher
{
    /// <summary>
    /// Broadcast a new post to the global activity feed
    /// </summary>
    Task BroadcastNewPostAsync(PostDto post, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast a new post to a specific group
    /// </summary>
    Task BroadcastNewGroupPostAsync(int groupId, PostDto post, CancellationToken cancellationToken = default);
}
