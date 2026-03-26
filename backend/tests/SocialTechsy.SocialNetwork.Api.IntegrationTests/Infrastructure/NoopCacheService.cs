using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

internal sealed class NoopCacheService : ICacheService
{
    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) =>
        await factory();

    public void Remove(string key)
    {
    }

    public void RemoveByPrefix(string prefix)
    {
    }
}
