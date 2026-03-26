using System.Text.Json.Serialization;

namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Chat;

public class MessageDto
{
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public long MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = null!;
    public string? SenderProfilePicture { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentDate { get; set; }
    public bool IsRead { get; set; }
    public string MessageType { get; set; } = "text";
    public string DeliveryStatus { get; set; } = "sent";
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public long? AttachmentSize { get; set; }

    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public long? ReplyToMessageId { get; set; }
    public ReplyToMessageDto? ReplyToMessage { get; set; }
    public List<MessageReactionDto> Reactions { get; set; } = new();
}
