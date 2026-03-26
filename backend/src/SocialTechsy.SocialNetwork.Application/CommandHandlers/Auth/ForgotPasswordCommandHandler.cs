using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly FrontendConfig _frontendConfig;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository resetTokenRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        IOptions<FrontendConfig> frontendConfig,
        IHostEnvironment environment,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _resetTokenRepository = resetTokenRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _frontendConfig = frontendConfig.Value;
        _environment = environment;
        _logger = logger;
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

        var resetLink = $"{_frontendConfig.BaseUrl}/reset-password?token={Uri.EscapeDataString(tokenString)}";

        try
        {
            var htmlBody = await _templateRenderer.RenderAsync(
                Domain.Enums.EmailTemplateType.PasswordReset,
                new
                {
                    UserName = user.DisplayName ?? user.Username,
                    ResetLink = resetLink,
                    Email = user.Email
                });

            await _emailService.SendEmailAsync(new EmailMessage
            {
                To = user.Email,
                Subject = _templateRenderer.GetSubject(Domain.Enums.EmailTemplateType.PasswordReset),
                HtmlBody = htmlBody,
                PlainTextBody = $"Hi {user.DisplayName ?? user.Username},\n\nClick the link to reset your password: {resetLink}\n\nThis link expires in 1 hour."
            }, cancellationToken);

            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            if (_environment.IsDevelopment())
            {
                throw;
            }
            return new ForgotPasswordResponse
            {
                Success = true,
                Message = "If that email exists in our system, a reset link has been generated.",
                ResetToken = tokenString,
                EmailSent = false
            };
        }

        return new ForgotPasswordResponse
        {
            Success = true,
            Message = "If that email exists in our system, a reset link has been generated.",
            ResetToken = tokenString,
            EmailSent = true
        };
    }
}

public class ForgotPasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ResetToken { get; set; }
    public bool EmailSent { get; set; } = true;
}
