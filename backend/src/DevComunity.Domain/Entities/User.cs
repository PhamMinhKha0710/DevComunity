namespace DevComunity.Domain.Entities;

/// <summary>
/// User entity - represents a registered user
/// </summary>
public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? ProfilePicture { get; set; }
    public int ReputationPoints { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }

    // Navigation properties
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public virtual ICollection<UserBadge> Badges { get; set; } = new List<UserBadge>();
}
