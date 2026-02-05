using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for TagPreference operations
/// </summary>
public interface ITagPreferenceRepository
{
    /// <summary>
    /// Get all tag preferences for a user
    /// </summary>
    Task<IEnumerable<TagPreference>> GetUserPreferencesAsync(int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get followed tags for a user
    /// </summary>
    Task<IEnumerable<TagPreference>> GetFollowedTagsAsync(int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get ignored tags for a user
    /// </summary>
    Task<IEnumerable<TagPreference>> GetIgnoredTagsAsync(int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a specific tag preference
    /// </summary>
    Task<TagPreference?> GetAsync(int userId, int tagId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add or update a tag preference
    /// </summary>
    Task<TagPreference> UpsertAsync(TagPreference preference, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove a tag preference
    /// </summary>
    Task<bool> DeleteAsync(int userId, int tagId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if user follows a tag
    /// </summary>
    Task<bool> IsFollowingAsync(int userId, int tagId, CancellationToken cancellationToken = default);
}
