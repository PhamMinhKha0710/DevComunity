namespace SocialTechsy.SocialNetwork.Domain.Entities;

public class RefreshToken
{
    public int RefreshTokenId { get; set; }
    public string Token { get; set; } = null!;
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    public virtual User User { get; set; } = null!;
}
