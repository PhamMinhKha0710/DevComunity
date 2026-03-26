using MediatR;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Commands.Friendships;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Friendships;

public class SendFriendRequestCommandHandler : IRequestHandler<SendFriendRequestCommand, FriendshipDto>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendFriendRequestCommandHandler> _logger;

    public SendFriendRequestCommandHandler(
        IFriendshipRepository friendshipRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<SendFriendRequestCommandHandler> logger)
    {
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FriendshipDto> Handle(SendFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var targetUser = await _userRepository.GetByIdAsync(request.AddresseeId, cancellationToken);
        if (targetUser == null)
            throw new InvalidOperationException("User not found");

        var existing = await _friendshipRepository.GetFriendshipAsync(request.RequesterId, request.AddresseeId, cancellationToken);
        if (existing != null)
        {
            if (existing.Status == FriendshipStatus.Accepted)
                throw new InvalidOperationException("Already friends");
            if (existing.Status == FriendshipStatus.Pending)
                throw new InvalidOperationException("Friend request already pending");
        }

        var friendship = Friendship.Create(request.RequesterId, request.AddresseeId);

        var created = await _friendshipRepository.AddAsync(friendship, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} sent friend request to {TargetId}", request.RequesterId, request.AddresseeId);

        var requesterUser = await _userRepository.GetByIdAsync(request.RequesterId, cancellationToken);

        try
        {
            var requester = requesterUser;
            var notification = new Notification
            {
                UserId = request.AddresseeId,
                Type = "FriendRequest",
                Message = $"{requester?.DisplayName ?? requester?.Username} sent you a friend request",
                Link = $"/friends",
                CreatedDate = DateTime.UtcNow
            };
            await _notificationDispatcher.DispatchAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend request notification");
        }

        return MapToDto(created, requesterUser!, targetUser);
    }

    private static FriendshipDto MapToDto(Friendship f, User requester, User addressee) => new()
    {
        FriendshipId = f.FriendshipId,
        Requester = new UserSummaryDto
        {
            UserId = requester.UserId,
            Username = requester.Username,
            DisplayName = requester.DisplayName,
            ProfilePicture = requester.ProfilePicture
        },
        Addressee = new UserSummaryDto
        {
            UserId = addressee.UserId,
            Username = addressee.Username,
            DisplayName = addressee.DisplayName,
            ProfilePicture = addressee.ProfilePicture
        },
        Status = f.Status.ToString(),
        CreatedAt = f.CreatedAt,
        RespondedAt = f.RespondedAt
    };
}

public class AcceptFriendRequestCommandHandler : IRequestHandler<AcceptFriendRequestCommand, FriendshipDto>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptFriendRequestCommandHandler> _logger;

    public AcceptFriendRequestCommandHandler(
        IFriendshipRepository friendshipRepository,
        IUserRepository userRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<AcceptFriendRequestCommandHandler> logger)
    {
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FriendshipDto> Handle(AcceptFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(request.FriendshipId, cancellationToken);
        if (friendship == null)
            throw new InvalidOperationException("Friend request not found");

        if (friendship.AddresseeId != request.UserId)
            throw new UnauthorizedAccessException("Not authorized");

        if (friendship.Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("Request is no longer pending");

        friendship.Accept();
        await _friendshipRepository.UpdateAsync(friendship, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} accepted friend request from {RequesterId}", request.UserId, friendship.RequesterId);

        try
        {
            var notification = new Notification
            {
                UserId = friendship.RequesterId,
                Type = "FriendAccepted",
                Message = $"{friendship.Addressee.DisplayName ?? friendship.Addressee.Username} accepted your friend request",
                Link = $"/friends",
                CreatedDate = DateTime.UtcNow
            };
            await _notificationDispatcher.DispatchAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend accepted notification");
        }

        return MapToDto(friendship);
    }

    private static FriendshipDto MapToDto(Friendship f) => new()
    {
        FriendshipId = f.FriendshipId,
        Requester = new UserSummaryDto
        {
            UserId = f.Requester.UserId,
            Username = f.Requester.Username,
            DisplayName = f.Requester.DisplayName,
            ProfilePicture = f.Requester.ProfilePicture
        },
        Addressee = new UserSummaryDto
        {
            UserId = f.Addressee.UserId,
            Username = f.Addressee.Username,
            DisplayName = f.Addressee.DisplayName,
            ProfilePicture = f.Addressee.ProfilePicture
        },
        Status = f.Status.ToString(),
        CreatedAt = f.CreatedAt,
        RespondedAt = f.RespondedAt
    };
}

public class RejectFriendRequestCommandHandler : IRequestHandler<RejectFriendRequestCommand, bool>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectFriendRequestCommandHandler> _logger;

    public RejectFriendRequestCommandHandler(
        IFriendshipRepository friendshipRepository,
        INotificationDispatcher notificationDispatcher,
        IUnitOfWork unitOfWork,
        ILogger<RejectFriendRequestCommandHandler> logger)
    {
        _friendshipRepository = friendshipRepository;
        _notificationDispatcher = notificationDispatcher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(RejectFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(request.FriendshipId, cancellationToken);
        if (friendship == null)
            throw new InvalidOperationException("Friend request not found");

        if (friendship.AddresseeId != request.UserId)
            throw new UnauthorizedAccessException("Not authorized");

        friendship.Reject();
        await _friendshipRepository.UpdateAsync(friendship, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} rejected friend request from {RequesterId}", request.UserId, friendship.RequesterId);

        try
        {
            var notification = new Notification
            {
                UserId = friendship.RequesterId,
                Type = "FriendRejected",
                Message = $"{friendship.Addressee.DisplayName ?? friendship.Addressee.Username} rejected your friend request",
                Link = $"/friends",
                CreatedDate = DateTime.UtcNow
            };
            await _notificationDispatcher.DispatchAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend rejected notification");
        }

        return true;
    }
}

public class DeleteFriendshipCommandHandler : IRequestHandler<DeleteFriendshipCommand, bool>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteFriendshipCommandHandler> _logger;

    public DeleteFriendshipCommandHandler(
        IFriendshipRepository friendshipRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteFriendshipCommandHandler> logger)
    {
        _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteFriendshipCommand request, CancellationToken cancellationToken)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(request.FriendshipId, cancellationToken);
        if (friendship == null)
            throw new InvalidOperationException("Friendship not found");

        if (friendship.RequesterId != request.UserId && friendship.AddresseeId != request.UserId)
            throw new UnauthorizedAccessException("Not authorized");

        await _friendshipRepository.DeleteAsync(request.FriendshipId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} deleted friendship {FriendshipId}", request.UserId, request.FriendshipId);

        return true;
    }
}
