using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Application.MessageBroker.Events;

namespace SocialTechsy.SocialNetwork.Infrastructure.MessageBroker.Consumers;

public class SendEmailNotificationConsumer : IConsumer<SendEmailNotificationEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SendEmailNotificationConsumer> _logger;

    public SendEmailNotificationConsumer(
        IServiceProvider serviceProvider,
        ILogger<SendEmailNotificationConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendEmailNotificationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "SendEmailNotificationConsumer: Sending email to {To} with subject '{Subject}'",
            message.To,
            message.Subject);

        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            await emailService.SendEmailAsync(new EmailMessage
            {
                To = message.To,
                Subject = message.Subject,
                HtmlBody = message.HtmlBody,
                PlainTextBody = message.PlainTextBody,
                FromName = message.FromName,
                ReplyTo = message.ReplyTo
            }, context.CancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {To}",
                message.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {To}. Subject: '{Subject}'",
                message.To,
                message.Subject);
            throw;
        }
    }
}
