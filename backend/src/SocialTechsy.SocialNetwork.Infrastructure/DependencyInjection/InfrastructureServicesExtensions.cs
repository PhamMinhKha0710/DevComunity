using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.External.Gitea;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using SocialTechsy.SocialNetwork.Infrastructure.Resilience;
using SocialTechsy.SocialNetwork.Infrastructure.Services;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.DependencyInjection;

public static class InfrastructureServicesExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthTokenIssuer, AuthTokenIssuer>();
        services.AddScoped<DataSeeder>();
        services.AddSingleton<ICacheService>(sp =>
            new CacheService(
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetService<IConnectionMultiplexer>()));

        services.AddResiliencePipelines();

        services.Configure<GiteaConfiguration>(
            configuration.GetSection(GiteaConfiguration.SectionName));
        services.AddHttpClient<IGiteaService, GiteaService>();
        services.AddScoped<IExternalGitService, ExternalGitServiceAdapter>();

        services.AddScoped<IExternalAuthService, ExternalAuthService>();

        return services;
    }
}
