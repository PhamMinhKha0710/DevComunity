namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Chat;

public class ConversationDto
{
    public int ConversationId { get; set; }
    public string? Title { get; set; }
    public bool IsGroupChat { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastMessageDate { get; set; }
    public string? LastMessagePreview { get; set; }
    public int UnreadCount { get; set; }
    public List<ConversationParticipantDto> Participants { get; set; } = new();
}
