using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class RequestForgotPasswordOtpCommandHandler
    : IRequestHandler<RequestForgotPasswordOtpCommand, RequestForgotPasswordOtpResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IForgotPasswordOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<RequestForgotPasswordOtpCommandHandler> _logger;

    public RequestForgotPasswordOtpCommandHandler(
        IUserRepository userRepository,
        IForgotPasswordOtpService otpService,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        IHostEnvironment environment,
        ILogger<RequestForgotPasswordOtpCommandHandler> logger)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _environment = environment;
        _logger = logger;
    }

    public async Task<RequestForgotPasswordOtpResponse> Handle(
        RequestForgotPasswordOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null)
        {
            return new RequestForgotPasswordOtpResponse
            {
                Success = true,
                Message = "If an account exists with that email, a verification code has been sent."
            };
        }

        string code;
        try
        {
            code = await _otpService.GenerateAndStoreOtpAsync(request.Email, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Forgot password OTP service unavailable");
            return new RequestForgotPasswordOtpResponse
            {
                Success = false,
                Message = "Code service is temporarily unavailable. Please try again later."
            };
        }

        try
        {
            var htmlBody = await _templateRenderer.RenderAsync(
                Domain.Enums.EmailTemplateType.PasswordChangeCode,
                new { UserName = user.DisplayName ?? user.Username, Code = code, Email = user.Email });

            await _emailService.SendEmailAsync(new EmailMessage
            {
                To = user.Email,
                Subject = _templateRenderer.GetSubject(Domain.Enums.EmailTemplateType.PasswordChangeCode),
                HtmlBody = htmlBody,
                PlainTextBody = $"Hi {user.DisplayName ?? user.Username},\n\nYour password reset code is: {code}\n\nThis code expires in 10 minutes. If you did not request a password reset, ignore this email."
            }, cancellationToken);

            _logger.LogInformation("Forgot password OTP sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send forgot password OTP to {Email}", user.Email);
            if (_environment.IsDevelopment())
            {
                throw;
            }
            return new RequestForgotPasswordOtpResponse
            {
                Success = false,
                Message = "Failed to send the verification code. Please try again."
            };
        }

        return new RequestForgotPasswordOtpResponse
        {
            Success = true,
            Message = "Verification code sent to your email."
        };
    }
}
