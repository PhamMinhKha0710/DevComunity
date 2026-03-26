using MediatR;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class ConfirmForgotPasswordCommandHandler
    : IRequestHandler<ConfirmForgotPasswordCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IForgotPasswordOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmForgotPasswordCommandHandler> _logger;

    public ConfirmForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IForgotPasswordOtpService otpService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(
        ConfirmForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Password must be at least 8 characters long."
            };
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Passwords do not match."
            };
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid or expired verification code."
            };
        }

        var isValid = await _otpService.ValidateAndDeleteAsync(request.Email, request.Code, cancellationToken);
        if (!isValid)
        {
            _logger.LogWarning("Invalid or expired forgot password OTP for {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid or expired verification code."
            };
        }

        user.UpdatePasswordHash(_passwordHasher.HashPassword(request.NewPassword));
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset successfully for {Email}", request.Email);

        return new AuthResponse
        {
            Success = true,
            Message = "Your password has been reset successfully. You can now sign in with your new password."
        };
    }
}
