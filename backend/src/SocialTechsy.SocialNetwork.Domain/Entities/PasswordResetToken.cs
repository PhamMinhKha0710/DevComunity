namespace SocialTechsy.SocialNetwork.Domain.Entities;

public class PasswordResetToken
{
    public int PasswordResetTokenId { get; set; }
    public string Token { get; set; } = null!;
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsUsed { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;

    public virtual User User { get; set; } = null!;
}
