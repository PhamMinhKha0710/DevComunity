using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SocialTechsy.SocialNetwork.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddCaching(configuration);
        services.AddMongoDb(configuration);
        services.AddMessageBroker(configuration);
        services.AddBackgroundJobs(configuration);
        services.AddIdGeneration(configuration);
        services.AddInfrastructureServices(configuration);

        return services;
    }
}
