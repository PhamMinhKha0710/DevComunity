using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SocialTechsy.SocialNetwork.Infrastructure.External.Gitea;

/// <summary>
/// Implementation of IGiteaService using HttpClient
/// </summary>
public class GiteaService : IGiteaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GiteaService> _logger;
    private readonly GiteaConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public bool IsConfigured => !string.IsNullOrEmpty(_config.BaseUrl);

    public GiteaService(
        HttpClient httpClient,
        IOptions<GiteaConfiguration> config,
        ILogger<GiteaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Configure HttpClient
        if (IsConfigured)
        {
            _httpClient.BaseAddress = new Uri(_config.BaseUrl.TrimEnd('/') + "/");
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
            
            if (!string.IsNullOrEmpty(_config.AccessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("token", _config.AccessToken);
            }
        }
    }

    public async Task<GiteaTreeResponse?> GetRepositoryTreeAsync(
        string owner, 
        string repo, 
        string sha = "HEAD",
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Gitea service is not configured");
            return null;
        }

        try
        {
            var url = $"repos/{owner}/{repo}/git/trees/{sha}";
            if (recursive)
            {
                url += "?recursive=true";
            }

            _logger.LogInformation("Fetching tree from Gitea: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gitea API returned {StatusCode} for tree request", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GiteaTreeResponse>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching repository tree from Gitea");
            return null;
        }
    }

    public async Task<GiteaContentResponse?> GetFileContentAsync(
        string owner, 
        string repo, 
        string path,
        string? branch = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Gitea service is not configured");
            return null;
        }

        try
        {
            var url = $"repos/{owner}/{repo}/contents/{path.TrimStart('/')}";
            if (!string.IsNullOrEmpty(branch))
            {
                url += $"?ref={branch}";
            }

            _logger.LogInformation("Fetching file content from Gitea: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gitea API returned {StatusCode} for content request", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GiteaContentResponse>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching file content from Gitea");
            return null;
        }
    }

    public async Task<List<GiteaContentResponse>> GetDirectoryContentsAsync(
        string owner, 
        string repo, 
        string? path = null,
        string? branch = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Gitea service is not configured");
            return new List<GiteaContentResponse>();
        }

        try
        {
            var url = $"repos/{owner}/{repo}/contents";
            if (!string.IsNullOrEmpty(path))
            {
                url += $"/{path.TrimStart('/')}";
            }
            if (!string.IsNullOrEmpty(branch))
            {
                url += $"?ref={branch}";
            }

            _logger.LogInformation("Fetching directory contents from Gitea: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gitea API returned {StatusCode} for directory request", response.StatusCode);
                return new List<GiteaContentResponse>();
            }

            var result = await response.Content.ReadFromJsonAsync<List<GiteaContentResponse>>(_jsonOptions, cancellationToken);
            return result ?? new List<GiteaContentResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching directory contents from Gitea");
            return new List<GiteaContentResponse>();
        }
    }

    public async Task<List<GiteaCommitResponse>> GetCommitsAsync(
        string owner, 
        string repo, 
        int page = 1, 
        int pageSize = 20,
        string? sha = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Gitea service is not configured");
            return new List<GiteaCommitResponse>();
        }

        try
        {
            var url = $"repos/{owner}/{repo}/commits?page={page}&limit={pageSize}";
            if (!string.IsNullOrEmpty(sha))
            {
                url += $"&sha={sha}";
            }

            _logger.LogInformation("Fetching commits from Gitea: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gitea API returned {StatusCode} for commits request", response.StatusCode);
                return new List<GiteaCommitResponse>();
            }

            var result = await response.Content.ReadFromJsonAsync<List<GiteaCommitResponse>>(_jsonOptions, cancellationToken);
            return result ?? new List<GiteaCommitResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching commits from Gitea");
            return new List<GiteaCommitResponse>();
        }
    }

    public async Task<GiteaCommitResponse?> GetCommitAsync(
        string owner, 
        string repo, 
        string sha,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Gitea service is not configured");
            return null;
        }

        try
        {
            var url = $"repos/{owner}/{repo}/git/commits/{sha}";

            _logger.LogInformation("Fetching commit from Gitea: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gitea API returned {StatusCode} for commit request", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GiteaCommitResponse>(_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching commit from Gitea");
            return null;
        }
    }
}
