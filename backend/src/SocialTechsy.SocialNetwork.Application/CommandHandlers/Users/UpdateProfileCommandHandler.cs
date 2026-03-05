using SocialTechsy.SocialNetwork.Application.Commands.Users;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Users;

public class UpdateProfileCommandHandler
{
    private readonly IUserRepository _userRepository;

    public UpdateProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> HandleAsync(int userId, UpdateProfileCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return false;

        if (command.DisplayName != null)
            user.DisplayName = command.DisplayName;
        if (command.Bio != null)
            user.Bio = command.Bio;
        if (command.Location != null)
            user.Location = command.Location;
        if (command.Website != null)
            user.Website = command.Website;

        await _userRepository.UpdateAsync(user, cancellationToken);
        return true;
    }
}
