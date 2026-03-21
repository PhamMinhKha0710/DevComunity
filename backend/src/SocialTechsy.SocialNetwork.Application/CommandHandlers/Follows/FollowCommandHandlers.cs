using MediatR;
using Microsoft.Extensions.Logging;
using MassTransit;
using SocialTechsy.SocialNetwork.Application.Commands.Follows;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.MessageBroker.Events;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Follows;

public class FollowCommandHandler : IRequestHandler<FollowCommand, bool>
{
    private readonly IFollowRepository _followRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<FollowCommandHandler> _logger;

    public FollowCommandHandler(
        IFollowRepository followRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<FollowCommandHandler> logger)
    {
        _followRepository = followRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<bool> Handle(FollowCommand request, CancellationToken cancellationToken)
    {
        var targetUser = await _userRepository.GetByIdAsync(request.FollowingId, cancellationToken);
        if (targetUser == null)
            throw new InvalidOperationException("User not found");

        var isFollowing = await _followRepository.IsFollowingAsync(request.FollowerId, request.FollowingId, cancellationToken);
        if (isFollowing)
            throw new InvalidOperationException("Already following this user");

        var follow = new UserFollow
        {
            FollowerId = request.FollowerId,
            FollowingId = request.FollowingId
        };

        await _followRepository.FollowAsync(follow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} followed user {TargetId}", request.FollowerId, request.FollowingId);

        try
        {
            var follower = await _userRepository.GetByIdAsync(request.FollowerId, cancellationToken);
            if (follower != null)
            {
                var notificationEvent = new NotificationCreatedEvent
                {
                    ActorId = request.FollowerId,
                    ReceiverId = request.FollowingId,
                    Type = "Follow",
                    Message = $"{follower.DisplayName ?? follower.Username} started following you",
                    Link = $"/users/{request.FollowerId}"
                };

                await _publishEndpoint.Publish(notificationEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing follow notification event");
        }

        return true;
    }
}

public class UnfollowCommandHandler : IRequestHandler<UnfollowCommand, bool>
{
    private readonly IFollowRepository _followRepository;
    private readonly ILogger<UnfollowCommandHandler> _logger;

    public UnfollowCommandHandler(
        IFollowRepository followRepository,
        ILogger<UnfollowCommandHandler> logger)
    {
        _followRepository = followRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(UnfollowCommand request, CancellationToken cancellationToken)
    {
        await _followRepository.UnfollowAsync(request.FollowerId, request.FollowingId, cancellationToken);

        _logger.LogInformation("User {UserId} unfollowed user {TargetId}", request.FollowerId, request.FollowingId);

        return true;
    }
}
