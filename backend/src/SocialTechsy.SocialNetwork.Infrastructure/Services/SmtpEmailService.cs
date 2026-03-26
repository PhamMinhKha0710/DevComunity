using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;
using Polly.Registry;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SmtpCommandException = MailKit.Net.Smtp.SmtpCommandException;
using SmtpProtoException = MailKit.Net.Smtp.SmtpProtocolException;

namespace SocialTechsy.SocialNetwork.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpConfig _config;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public SmtpEmailService(
        IOptions<SmtpConfig> config,
        ILogger<SmtpEmailService> logger,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _config = config.Value;
        _logger = logger;
        _resiliencePipeline = pipelineProvider.GetPipeline("smtp");
    }

    public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(
            string.IsNullOrEmpty(message.FromName) ? _config.FromName : message.FromName,
            _config.FromAddress));

        email.To.Add(MailboxAddress.Parse(message.To));

        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            email.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));
        }

        email.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody
        };

        email.Body = bodyBuilder.ToMessageBody();

        await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            using var client = new SmtpClient();

            var secureSocketOptions = _config.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(_config.Host, _config.Port, secureSocketOptions, ct);

            if (!string.IsNullOrEmpty(_config.Username))
            {
                await client.AuthenticateAsync(_config.Username, _config.Password, ct);
            }

            _logger.LogInformation(
                "Sending email to {To} with subject '{Subject}'",
                message.To,
                message.Subject);

            await client.SendAsync(email, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation(
                "Email sent successfully to {To}",
                message.To);
        }, cancellationToken);
    }

    public async Task SendEmailBatchAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            try
            {
                await SendEmailAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send batch email to {To}",
                    message.To);
            }
        }
    }
}
