namespace SocialTechsy.SocialNetwork.Infrastructure.External.Gitea;

/// <summary>
/// Configuration for Gitea API connection
/// </summary>
public class GiteaConfiguration
{
    public const string SectionName = "Gitea";
    
    /// <summary>
    /// Base URL of the Gitea instance API (e.g., https://gitea.example.com/api/v1)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Access token for Gitea API authentication
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Timeout in seconds for API requests
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
