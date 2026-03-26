namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task SendEmailBatchAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default);
}

public class EmailMessage
{
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string HtmlBody { get; set; }
    public required string PlainTextBody { get; set; }
    public string? FromName { get; set; }
    public string? ReplyTo { get; set; }
    public DateTime? ScheduledAt { get; set; }
}