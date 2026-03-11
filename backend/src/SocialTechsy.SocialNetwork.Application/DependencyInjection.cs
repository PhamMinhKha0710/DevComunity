using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Application.Common.Behaviors;

namespace SocialTechsy.SocialNetwork.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        return services;
    }
}
