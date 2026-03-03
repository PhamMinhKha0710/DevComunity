using Microsoft.Extensions.Caching.Memory;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly HashSet<string> _keys = new();
    private readonly object _lock = new();

    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(key, out T? cached))
            return cached;

        var value = await factory();
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };

        _cache.Set(key, value, options);
        lock (_lock) { _keys.Add(key); }
        return value;
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        lock (_lock) { _keys.Remove(key); }
    }

    public void RemoveByPrefix(string prefix)
    {
        List<string> keysToRemove;
        lock (_lock)
        {
            keysToRemove = _keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in keysToRemove) _keys.Remove(key);
        }
        foreach (var key in keysToRemove) _cache.Remove(key);
    }
}
