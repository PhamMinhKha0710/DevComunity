namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    void Remove(string key);
    void RemoveByPrefix(string prefix);
}
