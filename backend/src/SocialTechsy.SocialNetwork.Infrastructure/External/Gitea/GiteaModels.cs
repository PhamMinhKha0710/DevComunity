using System.Text.Json.Serialization;

namespace SocialTechsy.SocialNetwork.Infrastructure.External.Gitea;

#region Tree Responses

/// <summary>
/// Response from Gitea's git/trees endpoint
/// </summary>
public class GiteaTreeResponse
{
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("tree")]
    public List<GiteaTreeEntry> Tree { get; set; } = new();
    
    [JsonPropertyName("truncated")]
    public bool Truncated { get; set; }
}

/// <summary>
/// Single entry in a git tree
/// </summary>
public class GiteaTreeEntry
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
    
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;
    
    /// <summary>
    /// Type: "blob" for files, "tree" for directories
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("size")]
    public long Size { get; set; }
    
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

#endregion

#region Content Responses

/// <summary>
/// Response from Gitea's contents endpoint
/// </summary>
public class GiteaContentResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
    
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;
    
    /// <summary>
    /// Type: "file" or "dir"
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("size")]
    public long Size { get; set; }
    
    /// <summary>
    /// Base64 encoded content (only for files)
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("encoding")]
    public string? Encoding { get; set; }
    
    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
    
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
}

#endregion

#region Commit Responses

/// <summary>
/// Response from Gitea's commits endpoint
/// </summary>
public class GiteaCommitResponse
{
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("commit")]
    public GiteaCommitInfo Commit { get; set; } = new();
    
    [JsonPropertyName("author")]
    public GiteaUser? Author { get; set; }
    
    [JsonPropertyName("committer")]
    public GiteaUser? Committer { get; set; }
}

/// <summary>
/// Commit details
/// </summary>
public class GiteaCommitInfo
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("author")]
    public GiteaCommitAuthor Author { get; set; } = new();
    
    [JsonPropertyName("committer")]
    public GiteaCommitAuthor Committer { get; set; } = new();
}

/// <summary>
/// Author/Committer info from commit
/// </summary>
public class GiteaCommitAuthor
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
}

/// <summary>
/// Gitea user
/// </summary>
public class GiteaUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
    
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
    
    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;
}

#endregion
