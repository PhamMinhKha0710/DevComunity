using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, AuthResponse>
{
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository resetTokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _resetTokenRepository = resetTokenRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _resetTokenRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (storedToken == null)
        {
            return new AuthResponse { Success = false, Message = "Invalid reset token" };
        }

        if (storedToken.IsUsed)
        {
            return new AuthResponse { Success = false, Message = "This reset token has already been used" };
        }

        if (storedToken.IsExpired)
        {
            return new AuthResponse { Success = false, Message = "Reset token has expired. Please request a new one." };
        }

        var user = storedToken.User;
        if (user == null || !user.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResponse { Success = false, Message = "Invalid reset token" };
        }

        user.UpdatePasswordHash(_passwordHasher.HashPassword(request.NewPassword));
        await _userRepository.UpdateAsync(user, cancellationToken);

        storedToken.IsUsed = true;
        await _resetTokenRepository.UpdateAsync(storedToken, cancellationToken);
        await _resetTokenRepository.InvalidateAllByUserIdAsync(user.UserId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Success = true,
            Message = "Password has been reset successfully. Please login with your new password."
        };
    }
}
