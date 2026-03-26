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
        if (!redisEnabled)
        {
            services.AddSingleton<ILikeService, NoOpLikeService>();
            services.AddSingleton<IPasswordChangeCodeService, NoOpPasswordChangeCodeService>();
            services.AddSingleton<IForgotPasswordOtpService, NoOpForgotPasswordOtpService>();
            return services;
        }

        var redisConnStr = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        var options = new ConfigurationOptions
        {
            EndPoints = { redisConnStr },
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 3000
        };
        var multiplexer = ConnectionMultiplexer.Connect(options);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        services.AddSingleton<RedisChatCacheService>();
        services.AddSingleton<RedisPresenceService>();
        services.AddSingleton<RedisChatRateLimiter>();
        services.AddSingleton<RedisStreamWriteAheadLog>();
        services.AddSingleton<RedisLikeService>();
        services.AddSingleton<ILikeService>(sp => sp.GetRequiredService<RedisLikeService>());
        services.AddSingleton<RedisViewService>();
        services.AddSingleton<IViewService>(sp => sp.GetRequiredService<RedisViewService>());
        services.AddSingleton<PasswordChangeCodeService>();
        services.AddSingleton<IPasswordChangeCodeService>(sp => sp.GetRequiredService<PasswordChangeCodeService>());
        services.AddSingleton<ForgotPasswordOtpService>();
        services.AddSingleton<IForgotPasswordOtpService>(sp => sp.GetRequiredService<ForgotPasswordOtpService>());

        return services;
    }
}
