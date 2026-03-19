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

    public UpdateProfileCommandHandler(IUserRepository userRepository, ICacheService cacheService, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return false;

        user.UpdateProfile(request.DisplayName, request.Bio, request.Location, request.Website?.Trim());

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cacheService.RemoveByPrefix($"user:{request.UserId}");

        return true;
    }
}
