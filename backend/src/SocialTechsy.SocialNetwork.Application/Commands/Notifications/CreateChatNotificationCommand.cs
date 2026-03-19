using MediatR;

namespace SocialTechsy.SocialNetwork.Application.Commands.Notifications;

public class CreateChatNotificationCommand : IRequest
{
    public int ConversationId { get; set; }
    public long MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = null!;
    public List<int> RecipientIds { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
