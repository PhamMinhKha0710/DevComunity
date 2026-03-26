using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Domain.Entities;

public class EmailOutbox
{
    public int Id { get; set; }
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string HtmlBody { get; set; }
    public required string PlainTextBody { get; set; }
    public EmailTemplateType TemplateType { get; set; }
    public EmailOutboxStatus Status { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
}