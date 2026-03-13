namespace SocialTechsy.SocialNetwork.Domain.Entities;

public enum DeliveryStatus
{
    Sent = 0,
    Delivered = 1,
    Seen = 2
}

public class Message
{
    public static readonly string[] ValidMessageTypes = { "text", "image", "video", "audio", "file", "call" };
    public static readonly int MaxContentLength = 10_000;

    public long MessageId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime SentDate { get; set; }
    public string MessageType { get; set; } = "text";
    public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Sent;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public long? AttachmentSize { get; set; }

    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public long? ReplyToMessageId { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
    public virtual Message? ReplyToMessage { get; set; }
    public virtual ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();

    public bool IsMediaMessage => MessageType != "text";

    public static Message CreateText(int conversationId, int senderId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty.", nameof(content));
        if (content.Length > MaxContentLength)
            throw new ArgumentException($"Message exceeds maximum length of {MaxContentLength} characters.", nameof(content));

        return new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            MessageType = "text",
            SentDate = DateTime.UtcNow,
            IsRead = false
        };
    }

    public static Message CreateCallEvent(int conversationId, int senderId, string callEventType, string callType, int? durationSeconds = null)
    {
        var content = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = callEventType,
            callType,
            duration = durationSeconds
        });
        return new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            MessageType = "call",
            SentDate = DateTime.UtcNow,
            IsRead = false
        };
    }

    public static Message CreateMedia(int conversationId, int senderId, string messageType,
        string attachmentUrl, string? attachmentFileName, long attachmentSize, string? caption = null)
    {
        if (!ValidMessageTypes.Contains(messageType))
            throw new ArgumentException($"Invalid message type: {messageType}.", nameof(messageType));
        if (string.IsNullOrWhiteSpace(attachmentUrl))
            throw new ArgumentException("Attachment URL is required for media messages.", nameof(attachmentUrl));

        return new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = caption ?? "",
            MessageType = messageType,
            AttachmentUrl = attachmentUrl,
            AttachmentFileName = attachmentFileName,
            AttachmentSize = attachmentSize,
            SentDate = DateTime.UtcNow,
            IsRead = false
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }

    public string GetPreview(int maxLength = 50)
    {
        if (IsMediaMessage)
        {
            return MessageType switch
            {
                "image" => "Photo",
                "video" => "Video",
                "audio" => "Audio",
                "call" => "Call",
                _ => AttachmentFileName ?? "File"
            };
        }

        return Content.Length > maxLength ? Content[..maxLength] + "..." : Content;
    }

    public string GetReplyPreview(int maxLength = 100)
    {
        return Content.Length > maxLength ? Content[..maxLength] + "..." : Content;
    }
}
