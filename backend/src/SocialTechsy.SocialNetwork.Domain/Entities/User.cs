namespace SocialTechsy.SocialNetwork.Domain.Entities;

/// <summary>
/// User entity - represents a registered user
/// </summary>
public class User
{
    public int UserId { get; private set; }
    public string Username { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public string? Bio { get; private set; }
    public string? Location { get; private set; }
    public string? Website { get; private set; }
    public string? ProfilePicture { get; private set; }
    public int ReputationPoints { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? LastLoginDate { get; private set; }

    // External login (OAuth) fields
    public string? ExternalProvider { get; private set; }
    public string? ExternalProviderId { get; private set; }
    public string? ExternalProviderAvatar { get; private set; }

    // Navigation properties
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public virtual ICollection<UserBadge> Badges { get; set; } = new List<UserBadge>();

    private User() { }

    /// <summary>
    /// Creates a User instance for read-only projection use (e.g. MongoDB navigation).
    /// Properties are set directly for MongoDB mapping where EF Core tracking is not needed.
    /// </summary>
    public static User CreateProjected(int userId, string username, string? displayName, string? profilePicture)
    {
        return new User
        {
            UserId = userId,
            Username = username,
            Email = string.Empty,
            PasswordHash = string.Empty,
            DisplayName = displayName,
            ProfilePicture = profilePicture
        };
    }

    public static User Create(string username, string email, string passwordHash, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty.", nameof(username));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        return new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            DisplayName = displayName ?? username,
            CreatedDate = DateTime.UtcNow,
            ReputationPoints = 1,
            IsEmailVerified = false
        };
    }

    public void UpdateProfile(string? displayName, string? bio, string? location, string? website)
    {
        if (displayName != null && displayName.Length > 100)
            throw new ArgumentException("DisplayName cannot exceed 100 characters.", nameof(displayName));
        if (bio != null && bio.Length > 500)
            throw new ArgumentException("Bio cannot exceed 500 characters.", nameof(bio));
        if (location != null && location.Length > 100)
            throw new ArgumentException("Location cannot exceed 100 characters.", nameof(location));
        if (website != null && website.Length > 200)
            throw new ArgumentException("Website cannot exceed 200 characters.", nameof(website));

        if (displayName != null) DisplayName = displayName;
        if (bio != null) Bio = bio;
        if (location != null) Location = location;
        if (website != null) Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
    }

    public void UpdateReputation(int delta)
    {
        ReputationPoints += delta;
    }

    public void RecordLogin()
    {
        LastLoginDate = DateTime.UtcNow;
    }

    public void VerifyEmail() => IsEmailVerified = true;

    public void SetOAuthProvider(string provider, string providerId, string? avatarUrl)
    {
        ExternalProvider = provider;
        ExternalProviderId = providerId;
        ExternalProviderAvatar = avatarUrl;
    }

    public void LinkOAuthProvider(string provider, string providerId, string? avatarUrl)
    {
        ExternalProvider = provider;
        ExternalProviderId = providerId;
        ExternalProviderAvatar = avatarUrl;
        LastLoginDate = DateTime.UtcNow;
    }

    public void SetProfilePicture(string? url) => ProfilePicture = url;

    public void UpdatePasswordHash(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(newHash));
        PasswordHash = newHash;
    }
}
