using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Infrastructure.Email;

public class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<EmailTemplateRenderer> _logger;
    private readonly Dictionary<EmailTemplateType, string> _templatePaths;
    private readonly Dictionary<EmailTemplateType, string> _subjects;

    public EmailTemplateRenderer(
        IFileProvider fileProvider,
        ILogger<EmailTemplateRenderer> logger)
    {
        _fileProvider = fileProvider;
        _logger = logger;
        _templatePaths = new Dictionary<EmailTemplateType, string>
        {
            [EmailTemplateType.PasswordReset] = "Email/Templates/PasswordReset.html",
            [EmailTemplateType.EmailVerification] = "Email/Templates/EmailVerification.html",
            [EmailTemplateType.AccountLocked] = "Email/Templates/AccountLocked.html",
            [EmailTemplateType.Welcome] = "Email/Templates/Welcome.html",
            [EmailTemplateType.PasswordChangeCode] = "Email/Templates/PasswordChangeCode.html",
        };
        _subjects = new Dictionary<EmailTemplateType, string>
        {
            [EmailTemplateType.PasswordReset] = "Reset Your Password - SocialTechsy",
            [EmailTemplateType.EmailVerification] = "Verify Your Email - SocialTechsy",
            [EmailTemplateType.AccountLocked] = "Account Security Alert - SocialTechsy",
            [EmailTemplateType.Welcome] = "Welcome to SocialTechsy!",
            [EmailTemplateType.PasswordChangeCode] = "Your Password Change Code - SocialTechsy",
        };
    }

    public string GetSubject(EmailTemplateType template)
    {
        return _subjects.GetValueOrDefault(template, "SocialTechsy Notification");
    }

    public async Task<string> RenderAsync(EmailTemplateType template, object model)
    {
        if (!_templatePaths.TryGetValue(template, out var path))
        {
            _logger.LogWarning("Email template not found for type {TemplateType}", template);
            return string.Empty;
        }

        try
        {
            var fileInfo = _fileProvider.GetFileInfo(path);
            if (!fileInfo.Exists)
            {
                _logger.LogWarning("Email template file not found: {Path}", path);
                return GetDefaultTemplate(template, model);
            }

            using var reader = new StreamReader(fileInfo.CreateReadStream());
            var content = await reader.ReadToEndAsync();

            var properties = model.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(model)?.ToString() ?? "";
                content = content.Replace($"{{{{{prop.Name}}}}}", value);
            }

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render email template {TemplateType}", template);
            return GetDefaultTemplate(template, model);
        }
    }

    private static string GetDefaultTemplate(EmailTemplateType template, object model)
    {
        var props = model.GetType().GetProperties();
        var dict = props.ToDictionary(p => p.Name, p => p.GetValue(model)?.ToString() ?? "");
        var body = string.Join("\n", dict.Select(kv => $"{kv.Key}: {kv.Value}"));
        return $"""
            <html><body>
            <h1>{template}</h1>
            <pre>{body}</pre>
            </body></html>
            """;
    }
}
