using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Polly.Registry;
using SocialTechsy.SocialNetwork.Application;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.Email;
using SocialTechsy.SocialNetwork.Infrastructure.MessageBroker.Consumers;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;
using SocialTechsy.SocialNetwork.Infrastructure.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure.DependencyInjection;

public static class EmailExtensions
{
    public const string SectionName = "Email";

    public static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var emailSection = configuration.GetSection(SectionName);
        services.Configure<SmtpConfig>(emailSection);

        var frontendSection = configuration.GetSection(FrontendConfig.SectionName);
        services.Configure<FrontendConfig>(frontendSection);

        var assemblyLocation = Path.GetDirectoryName(typeof(EmailExtensions).Assembly.Location) ?? "";
        var fileProvider = new PhysicalFileProvider(assemblyLocation);

        services.AddSingleton<IEmailTemplateRenderer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<EmailTemplateRenderer>>();
            return new EmailTemplateRenderer(fileProvider, logger);
        });

        services.AddScoped<IEmailService>(sp =>
        {
            var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SmtpConfig>>();
            var logger = sp.GetRequiredService<ILogger<SmtpEmailService>>();
            var pipelineProvider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();
            return new SmtpEmailService(config, logger, pipelineProvider);
        });

        services.AddScoped<IEmailOutboxRepository, EmailOutboxRepository>();

        return services;
    }
}
