using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Users;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Users;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;

    public UpdateProfileCommandHandler(IUserRepository userRepository, ICacheService cacheService)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
    }

    public async Task<bool> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return false;

        if (request.DisplayName != null)
            user.DisplayName = request.DisplayName;
        if (request.Bio != null)
            user.Bio = request.Bio;
        if (request.Location != null)
            user.Location = request.Location;
        if (request.Website != null)
            user.Website = request.Website;

        await _userRepository.UpdateAsync(user, cancellationToken);

        _cacheService.RemoveByPrefix($"user:{request.UserId}");

        return true;
    }
}
