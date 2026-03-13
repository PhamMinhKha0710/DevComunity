namespace SocialTechsy.SocialNetwork.Domain.Entities;

public class Conversation
{
    public static readonly string[] ValidReactionTypes = { "like", "love", "haha", "wow", "sad", "angry" };

    public int ConversationId { get; set; }
    public string? Title { get; set; }
    public bool IsGroupChat { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastMessageDate { get; set; }

    public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public static Conversation Create(bool isGroupChat, string? title = null)
    {
        return new Conversation
        {
            IsGroupChat = isGroupChat,
            Title = title,
            CreatedDate = DateTime.UtcNow
        };
    }

    public bool HasParticipant(int userId) =>
        Participants.Any(p => p.UserId == userId);

    public void EnsureParticipant(int userId)
    {
        if (!HasParticipant(userId))
            throw new UnauthorizedAccessException($"User {userId} is not a participant in conversation {ConversationId}.");
    }

    public ConversationParticipant AddParticipant(int userId)
    {
        if (HasParticipant(userId))
            throw new InvalidOperationException($"User {userId} is already a participant.");

        var participant = new ConversationParticipant
        {
            ConversationId = ConversationId,
            UserId = userId,
            JoinedDate = DateTime.UtcNow
        };
        Participants.Add(participant);
        return participant;
    }

    public Message CreateMessage(int senderId, string content, string messageType = "text",
        string? attachmentUrl = null, string? attachmentFileName = null, long? attachmentSize = null,
        int? replyToMessageId = null)
    {
        EnsureParticipant(senderId);

        if (messageType == "text" && string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Text message content cannot be empty.");

        var message = Message.CreateText(ConversationId, senderId, content);

        if (messageType != "text")
        {
            message.MessageType = messageType;
            message.AttachmentUrl = attachmentUrl;
            message.AttachmentFileName = attachmentFileName;
            message.AttachmentSize = attachmentSize ?? 0;
        }

        message.ReplyToMessageId = replyToMessageId;
        LastMessageDate = message.SentDate;
        return message;
    }

    public IEnumerable<int> GetOtherParticipantIds(int excludeUserId) =>
        Participants.Where(p => p.UserId != excludeUserId).Select(p => p.UserId);
}
