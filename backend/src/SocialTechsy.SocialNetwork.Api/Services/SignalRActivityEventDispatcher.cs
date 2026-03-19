using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Api.Hubs;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Api.Services;

public class SignalRActivityEventDispatcher : IActivityEventDispatcher
{
    private readonly IHubContext<ActivityHub> _hubContext;
    private readonly ILogger<SignalRActivityEventDispatcher> _logger;

    public SignalRActivityEventDispatcher(
        IHubContext<ActivityHub> hubContext,
        ILogger<SignalRActivityEventDispatcher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastNewPostAsync(PostDto post, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group("activity_feed")
                .SendAsync("NewPost", post, cancellationToken);
            _logger.LogInformation("Broadcasted new post {PostId} to activity feed", post.PostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast new post {PostId}", post.PostId);
        }
    }

    public async Task BroadcastNewGroupPostAsync(int groupId, PostDto post, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group($"group_{groupId}")
                .SendAsync("NewGroupPost", post, cancellationToken);
            _logger.LogInformation("Broadcasted new post {PostId} to group {GroupId}", post.PostId, groupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast new post {PostId} to group {GroupId}", post.PostId, groupId);
        }
    }
}
