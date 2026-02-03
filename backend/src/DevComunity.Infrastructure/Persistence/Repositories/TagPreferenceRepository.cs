using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Domain.Entities;
using DevComunity.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DevComunity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for TagPreference operations
/// </summary>
public class TagPreferenceRepository : ITagPreferenceRepository
{
    private readonly DevComunityDbContext _context;

    public TagPreferenceRepository(DevComunityDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TagPreference>> GetUserPreferencesAsync(
        int userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.TagPreferences
            .Include(tp => tp.Tag)
            .Where(tp => tp.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TagPreference>> GetFollowedTagsAsync(
        int userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.TagPreferences
            .Include(tp => tp.Tag)
            .Where(tp => tp.UserId == userId && tp.IsFollowed)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TagPreference>> GetIgnoredTagsAsync(
        int userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.TagPreferences
            .Include(tp => tp.Tag)
            .Where(tp => tp.UserId == userId && tp.IsIgnored)
            .ToListAsync(cancellationToken);
    }

    public async Task<TagPreference?> GetAsync(
        int userId, 
        int tagId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.TagPreferences
            .Include(tp => tp.Tag)
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TagId == tagId, cancellationToken);
    }

    public async Task<TagPreference> UpsertAsync(
        TagPreference preference, 
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.TagPreferences
            .FirstOrDefaultAsync(tp => tp.UserId == preference.UserId && tp.TagId == preference.TagId, cancellationToken);

        if (existing != null)
        {
            existing.IsFollowed = preference.IsFollowed;
            existing.IsIgnored = preference.IsIgnored;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.TagPreferences.Update(existing);
        }
        else
        {
            preference.CreatedAt = DateTime.UtcNow;
            await _context.TagPreferences.AddAsync(preference, cancellationToken);
            existing = preference;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(
        int userId, 
        int tagId, 
        CancellationToken cancellationToken = default)
    {
        var preference = await _context.TagPreferences
            .FirstOrDefaultAsync(tp => tp.UserId == userId && tp.TagId == tagId, cancellationToken);

        if (preference == null)
            return false;

        _context.TagPreferences.Remove(preference);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> IsFollowingAsync(
        int userId, 
        int tagId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.TagPreferences
            .AnyAsync(tp => tp.UserId == userId && tp.TagId == tagId && tp.IsFollowed, cancellationToken);
    }
}
