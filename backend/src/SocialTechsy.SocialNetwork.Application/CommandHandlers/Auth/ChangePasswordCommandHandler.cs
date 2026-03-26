using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordChangeCodeService _codeService;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPasswordChangeCodeService codeService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _codeService = codeService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "User not found." };
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "OAuth users cannot change their password through this method."
            };
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return new AuthResponse { Success = false, Message = "Current password is incorrect." };
        }

        var codeValid = await _codeService.ValidateAndDeleteAsync(request.UserId, request.VerificationCode, cancellationToken);
        if (!codeValid)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid or expired verification code. Please request a new one."
            };
        }

        user.UpdatePasswordHash(_passwordHasher.HashPassword(request.NewPassword));
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Success = true,
            Message = "Password changed successfully."
        };
    }
}
