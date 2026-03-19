using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Infrastructure.IdGeneration;

namespace SocialTechsy.SocialNetwork.Infrastructure.DependencyInjection;

public static class IdGenerationExtensions
{
    public static IServiceCollection AddIdGeneration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serverId = configuration.GetValue("Snowflake:ServerId", 0);
        services.AddSingleton(new SnowflakeIdGenerator(serverId));

        return services;
    }
}
