using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.Caching;

namespace SocialTechsy.SocialNetwork.Infrastructure.DependencyInjection;

public static class CachingExtensions
{
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisEnabled = configuration.GetValue<bool>("Redis:Enabled");
        if (!redisEnabled) return services;

        var redisConnStr = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnStr));

        services.AddSingleton<RedisChatCacheService>();
        services.AddSingleton<RedisPresenceService>();
        services.AddSingleton<RedisChatRateLimiter>();
        services.AddSingleton<RedisStreamWriteAheadLog>();
        services.AddSingleton<RedisLikeService>();
        services.AddSingleton<ILikeService>(sp => sp.GetRequiredService<RedisLikeService>());
        services.AddSingleton<RedisViewService>();
        services.AddSingleton<IViewService>(sp => sp.GetRequiredService<RedisViewService>());

        return services;
    }
}
