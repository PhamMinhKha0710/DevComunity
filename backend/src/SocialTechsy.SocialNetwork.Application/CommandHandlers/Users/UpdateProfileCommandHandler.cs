using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Users;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Users;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaOwnershipChecker _mediaOwnershipChecker;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        IMediaOwnershipChecker mediaOwnershipChecker)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _mediaOwnershipChecker = mediaOwnershipChecker;
    }

    public async Task<bool> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return false;

        user.UpdateProfile(request.DisplayName, request.Bio, request.Location, request.Website?.Trim());

        if (!string.IsNullOrWhiteSpace(request.ProfilePicture))
        {
            var validUrl = await ValidateProfilePictureUrlAsync(request.ProfilePicture, request.UserId, cancellationToken);
            if (!validUrl)
                return false;
            user.SetProfilePicture(request.ProfilePicture);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.RemoveByPrefix($"user:{request.UserId}");

        return true;
    }

    private async Task<bool> ValidateProfilePictureUrlAsync(string url, int userId, CancellationToken ct)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return true;
        }

        if (url.StartsWith("/api/media/", StringComparison.OrdinalIgnoreCase))
        {
            return await _mediaOwnershipChecker.IsOwnerAsync(url, userId, ct);
        }

        return false;
    }
}
