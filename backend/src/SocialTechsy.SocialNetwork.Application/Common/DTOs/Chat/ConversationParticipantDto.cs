namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Chat;

public class ConversationParticipantDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
}
