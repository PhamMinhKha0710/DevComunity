namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Chat;

public class MessageReactionDto
{
    public int MessageReactionId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? ProfilePicture { get; set; }
    public string ReactionType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
