using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialTechsy.SocialNetwork.Application;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly FrontendConfig _frontendConfig;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        IOptions<FrontendConfig> frontendConfig,
        ILogger<RegisterCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _frontendConfig = frontendConfig.Value;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Email already in use"
            };
        }

        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Username already taken"
            };
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var user = User.Create(
                request.Username,
                request.Email,
                _passwordHasher.HashPassword(request.Password),
                request.DisplayName);

            var createdUser = await _userRepository.AddAsync(user, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            await _unitOfWork.SaveChangesAsync(ct);

            try
            {
                var dashboardLink = $"{_frontendConfig.BaseUrl}/dashboard";
                var htmlBody = await _templateRenderer.RenderAsync(
                    EmailTemplateType.Welcome,
                    new
                    {
                        UserName = createdUser.DisplayName ?? createdUser.Username,
                        DashboardLink = dashboardLink
                    });

                await _emailService.SendEmailAsync(new EmailMessage
                {
                    To = createdUser.Email,
                    Subject = _templateRenderer.GetSubject(EmailTemplateType.Welcome),
                    HtmlBody = htmlBody,
                    PlainTextBody = $"Welcome to SocialTechsy, {createdUser.DisplayName ?? createdUser.Username}! Start exploring questions and answers on your dashboard."
                }, ct);

                _logger.LogInformation("Welcome email sent to {Email}", createdUser.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", createdUser.Email);
            }

            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful. Please log in with your credentials.",
                User = new UserDto
                {
                    UserId = createdUser.UserId,
                    Username = createdUser.Username,
                    Email = createdUser.Email,
                    DisplayName = createdUser.DisplayName,
                    ReputationPoints = createdUser.ReputationPoints,
                    IsEmailVerified = createdUser.IsEmailVerified
                }
            };
        }, cancellationToken);
    }
}
