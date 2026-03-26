using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialTechsy.SocialNetwork.Application;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;

public class RequestPasswordChangeCodeCommandHandler
    : IRequestHandler<RequestPasswordChangeCodeCommand, RequestPasswordChangeCodeResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IPasswordChangeCodeService _codeService;
    private readonly ICacheService _cacheService;
    private readonly FrontendConfig _frontendConfig;
    private readonly ILogger<RequestPasswordChangeCodeCommandHandler> _logger;
    private static readonly Dictionary<int, (int Count, DateTime WindowStart)> _localRateLimit = new();
    private const int MaxRequestsPerHour = 3;

    public RequestPasswordChangeCodeCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        IPasswordChangeCodeService codeService,
        ICacheService cacheService,
        IOptions<FrontendConfig> frontendConfig,
        ILogger<RequestPasswordChangeCodeCommandHandler> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _codeService = codeService;
        _cacheService = cacheService;
        _frontendConfig = frontendConfig.Value;
        _logger = logger;
    }

    public async Task<RequestPasswordChangeCodeResponse> Handle(
        RequestPasswordChangeCodeCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddHours(-1);

        lock (_localRateLimit)
        {
            if (_localRateLimit.TryGetValue(request.UserId, out var entry) && entry.WindowStart > windowStart)
            {
                if (entry.Count >= MaxRequestsPerHour)
                {
                    _logger.LogWarning("Rate limit exceeded for user {UserId}", request.UserId);
                    return new RequestPasswordChangeCodeResponse
                    {
                        Success = false,
                        Message = "Too many requests. Please try again later."
                    };
                }
                _localRateLimit[request.UserId] = (entry.Count + 1, entry.WindowStart);
            }
            else
            {
                _localRateLimit[request.UserId] = (1, now);
            }
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return new RequestPasswordChangeCodeResponse { Success = false, Message = "User not found." };

        string code;
        try
        {
            code = await _codeService.GenerateAndStoreCodeAsync(request.UserId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Password change code service unavailable");
            return new RequestPasswordChangeCodeResponse { Success = false, Message = "Code service unavailable." };
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
                PlainTextBody = $"Hi {user.DisplayName ?? user.Username},\n\nYour password change code is: {code}\n\nThis code expires in 10 minutes. If you did not request a password change, ignore this email."
            }, cancellationToken);

            _logger.LogInformation("Password change code sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password change code email to {Email}", user.Email);
        }

        return new RequestPasswordChangeCodeResponse
        {
            Success = true,
            Message = "Verification code sent to your email."
        };
    }
}
