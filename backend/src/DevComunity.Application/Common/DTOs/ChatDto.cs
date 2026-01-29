namespace DevComunity.Application.Common.DTOs;

/// <summary>
/// DTO for conversation list response
/// </summary>
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

/// <summary>
/// DTO for conversation participant
/// </summary>
public class ConversationParticipantDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
}

/// <summary>
/// DTO for message response
/// </summary>
public class MessageDto
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = null!;
    public string? SenderProfilePicture { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentDate { get; set; }
    public bool IsRead { get; set; }
}
