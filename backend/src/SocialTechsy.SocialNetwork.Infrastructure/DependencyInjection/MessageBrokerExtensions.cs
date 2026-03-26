using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.BackgroundJobs;
using SocialTechsy.SocialNetwork.Infrastructure.MessageBroker.Consumers;
using SocialTechsy.SocialNetwork.Infrastructure.MessageBroker.RabbitMQ;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.DependencyInjection;

public static class MessageBrokerExtensions
{
    public static IServiceCollection AddMessageBroker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitSection = configuration.GetSection(RabbitMqSettings.SectionName);
        var rabbitEnabled = rabbitSection.GetValue<bool>("Enabled");
        if (!rabbitEnabled) return services;

        services.Configure<RabbitMqSettings>(rabbitSection);

        services.AddMassTransit(x =>
        {
            x.AddConsumer<NotificationCreatedConsumer>();
            x.AddConsumer<SendEmailNotificationConsumer>();
            x.UsingRabbitMq((context, cfg) =>
            {
                var hostName = configuration[$"{RabbitMqSettings.SectionName}:HostName"] ?? "localhost";
                var port = configuration.GetValue<ushort>($"{RabbitMqSettings.SectionName}:Port", 5672);
                var vHost = configuration[$"{RabbitMqSettings.SectionName}:VirtualHost"] ?? "/";

                cfg.Host(hostName, port, vHost, h =>
                {
                    h.Username(configuration[$"{RabbitMqSettings.SectionName}:UserName"] ?? "guest");
                    h.Password(configuration[$"{RabbitMqSettings.SectionName}:Password"] ?? "guest");
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddSingleton<IConnection>(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration[$"{RabbitMqSettings.SectionName}:HostName"] ?? "localhost",
                Port = configuration.GetValue($"{RabbitMqSettings.SectionName}:Port", 5672),
                UserName = configuration[$"{RabbitMqSettings.SectionName}:UserName"] ?? "guest",
                Password = configuration[$"{RabbitMqSettings.SectionName}:Password"] ?? "guest",
                VirtualHost = configuration[$"{RabbitMqSettings.SectionName}:VirtualHost"] ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                TopologyRecoveryEnabled = true,
                ConsumerDispatchConcurrency = 2
            };
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddSingleton<IChatMessageBroker, RabbitMqChatMessageBroker>();
        services.AddSingleton<ISocialEventPublisher, SocialEventPublisher>();

        return services;
    }

    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitSection = configuration.GetSection(RabbitMqSettings.SectionName);
        var rabbitEnabled = rabbitSection.GetValue<bool>("Enabled");
        if (!rabbitEnabled) return services;

        services.AddHostedService(sp => new ChatMessageConsumerService(
            sp.GetRequiredService<IConnection>(),
            sp,
            sp.GetRequiredService<ILogger<ChatMessageConsumerService>>(),
            sp.GetService<IConnectionMultiplexer>()));

        services.AddHostedService(sp => new LikeNotificationConsumer(
            sp.GetRequiredService<IConnection>(),
            sp,
            sp.GetRequiredService<ILogger<LikeNotificationConsumer>>()));

        services.AddHostedService<SocialTechsy.SocialNetwork.Infrastructure.MongoDB.OutboxProcessor>();
        services.AddHostedService(sp => new SqlOutboxProcessor(
            sp,
            sp.GetRequiredService<IConnection>(),
            sp.GetRequiredService<ILogger<SqlOutboxProcessor>>()));

        services.AddHostedService<ViewSyncBackgroundService>();

        return services;
    }
}
