using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Services;

/// <summary>
/// Redis-backed distributed cache with in-process IMemoryCache as L1 fallback.
/// When Redis is unavailable, degrades to local memory cache only.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IMemoryCache _localCache;
    private readonly IDatabase? _redis;
    private readonly HashSet<string> _localKeys = new();
    private readonly object _lock = new();

    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LocalCacheDuration = TimeSpan.FromSeconds(30);

    public CacheService(IMemoryCache cache, IConnectionMultiplexer? redis = null)
    {
        _localCache = cache;
        _redis = redis?.GetDatabase();
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (_localCache.TryGetValue(key, out T? localCached))
            return localCached;

        var ttl = expiration ?? DefaultExpiration;

        if (_redis != null)
        {
            try
            {
                var redisVal = await _redis.StringGetAsync($"cache:{key}");
                if (redisVal.HasValue)
                {
                    var deserialized = JsonSerializer.Deserialize<T>(redisVal!);
                    SetLocal(key, deserialized, LocalCacheDuration);
                    return deserialized;
                }
            }
            catch { /* fall through to factory */ }
        }

        var value = await factory();

        if (_redis != null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _redis.StringSetAsync($"cache:{key}", json, ttl);
            }
            catch { /* Redis unavailable, local-only */ }
        }

        SetLocal(key, value, ttl < LocalCacheDuration ? ttl : LocalCacheDuration);
        return value;
    }

    public void Remove(string key)
    {
        _localCache.Remove(key);
        lock (_lock) { _localKeys.Remove(key); }

        if (_redis != null)
        {
            try { _redis.KeyDelete($"cache:{key}"); }
            catch { /* best-effort */ }
        }
    }

    public void RemoveByPrefix(string prefix)
    {
        List<string> keysToRemove;
        lock (_lock)
        {
            keysToRemove = _localKeys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var k in keysToRemove) _localKeys.Remove(k);
        }
        foreach (var k in keysToRemove) _localCache.Remove(k);
    }

    private void SetLocal<T>(string key, T? value, TimeSpan duration)
    {
        var opts = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration
        };
        _localCache.Set(key, value, opts);
        lock (_lock) { _localKeys.Add(key); }
    }
}
