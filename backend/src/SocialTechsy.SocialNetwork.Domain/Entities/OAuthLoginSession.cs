namespace SocialTechsy.SocialNetwork.Domain.Entities;

/// <summary>
/// Short-lived session storing OAuth tokens for exchange.
/// Used to avoid exposing tokens in redirect URLs.
/// </summary>
public class OAuthLoginSession
{
    public int OAuthLoginSessionId { get; set; }
    public string Code { get; set; } = null!;
    public int UserId { get; set; }
    public string Provider { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;

    public virtual User User { get; set; } = null!;
}
