namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

/// <summary>
/// Handles SignalR push for chat messages. Implemented in the API layer
/// where SignalR hub contexts are available.
/// </summary>
public interface IChatPushHandler
{
    Task PushMessageAsync(ChatPushEvent pushEvent);
    Task PushNewMessageNotificationAsync(ChatPushEvent pushEvent);
}

public class ChatPushEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public int ConversationId { get; set; }
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = "";
    public string? SenderDisplayName { get; set; }
    public string? SenderProfilePicture { get; set; }
    public string Content { get; set; } = "";
    public string MessageType { get; set; } = "text";
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public long? AttachmentSize { get; set; }
    public DateTime SentDate { get; set; }
    public int? ReplyToMessageId { get; set; }
    public string NotificationPreview { get; set; } = "";
    public List<int> RecipientUserIds { get; set; } = new();
}
