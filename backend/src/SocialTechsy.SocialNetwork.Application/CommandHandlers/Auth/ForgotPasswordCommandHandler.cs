using System.Security.Cryptography;
using MediatR;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository resetTokenRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _resetTokenRepository = resetTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null)
        {
            return new ForgotPasswordResponse
            {
                Success = true,
                Message = "If that email exists in our system, a reset link has been generated."
            };
        }

        await _resetTokenRepository.InvalidateAllByUserIdAsync(user.UserId, cancellationToken);

        var tokenBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var tokenString = Convert.ToBase64String(tokenBytes);

        var resetToken = new PasswordResetToken
        {
            Token = tokenString,
            UserId = user.UserId,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };

        await _resetTokenRepository.AddAsync(resetToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ForgotPasswordResponse
        {
            Success = true,
            Message = "If that email exists in our system, a reset link has been generated.",
            ResetToken = tokenString
        };
    }
}

public class ForgotPasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ResetToken { get; set; }
}
