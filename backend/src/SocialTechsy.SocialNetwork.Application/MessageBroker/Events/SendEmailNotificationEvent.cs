namespace SocialTechsy.SocialNetwork.Application.MessageBroker.Events;

public record SendEmailNotificationEvent
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public required string PlainTextBody { get; init; }
    public string? FromName { get; init; }
    public string? ReplyTo { get; init; }
}
