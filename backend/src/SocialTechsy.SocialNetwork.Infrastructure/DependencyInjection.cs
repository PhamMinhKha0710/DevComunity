using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;
using StackExchange.Redis;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.External.Gitea;
using SocialTechsy.SocialNetwork.Infrastructure.MongoDB;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Repositories;
using SocialTechsy.SocialNetwork.Infrastructure.RabbitMQ;
using SocialTechsy.SocialNetwork.Infrastructure.Redis;
using SocialTechsy.SocialNetwork.Infrastructure.IdGeneration;
using SocialTechsy.SocialNetwork.Infrastructure.Resilience;
using SocialTechsy.SocialNetwork.Infrastructure.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database context (SQL Server - used for all non-chat entities)
        services.AddSingleton<OutboxSaveChangesInterceptor>();
        services.AddDbContext<SocialTechsySocialNetworkDbContext>((sp, options) =>
            options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(SocialTechsySocialNetworkDbContext).Assembly.FullName)
                          .EnableRetryOnFailure(3))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>()));

        var fullTextEnabled = configuration.GetValue<bool>("Search:FullTextEnabled");
        services.AddScoped<IQuestionRepository>(sp =>
            new QuestionRepository(
                sp.GetRequiredService<SocialTechsySocialNetworkDbContext>(),
                sp.GetRequiredService<ITagRepository>(),
                fullTextEnabled));
        services.AddScoped<IAnswerRepository, AnswerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVoteRepository, VoteRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ISavedItemRepository, SavedItemRepository>();
        services.AddScoped<IBadgeRepository, BadgeRepository>();
        services.AddScoped<ICodeRepository, CodeRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // ========== REDIS CACHE ==========
        var redisEnabled = configuration.GetValue<bool>("Redis:Enabled");
        if (redisEnabled)
        {
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
            services.AddHostedService<ViewSyncBackgroundService>();
        }

        // ========== SNOWFLAKE ID GENERATOR ==========
        var serverId = configuration.GetValue("Snowflake:ServerId", 0);
        services.AddSingleton(new SnowflakeIdGenerator(serverId));

        // ========== CHAT REPOSITORY (MongoDB + Redis cache) ==========
        var mongoSection = configuration.GetSection(MongoDbSettings.SectionName);
        if (mongoSection.Exists() && !string.IsNullOrEmpty(mongoSection["ConnectionString"]))
        {
            services.Configure<MongoDbSettings>(mongoSection);
            services.AddSingleton<MongoClient>(sp =>
            {
                var connStr = configuration[$"{MongoDbSettings.SectionName}:ConnectionString"]!;
                return new MongoClient(connStr);
            });
            services.AddSingleton<IMongoDatabase>(sp =>
            {
                var client = sp.GetRequiredService<MongoClient>();
                var dbName = configuration[$"{MongoDbSettings.SectionName}:DatabaseName"] ?? "SocialTechsyChatDb";
                return client.GetDatabase(dbName);
            });
            services.AddScoped<IChatRepository>(sp =>
            {
                var client = sp.GetRequiredService<MongoClient>();
                var database = sp.GetRequiredService<IMongoDatabase>();
                var userRepo = sp.GetRequiredService<IUserRepository>();
                var snowflake = sp.GetRequiredService<SnowflakeIdGenerator>();
                var cache = sp.GetService<RedisChatCacheService>();
                return new MongoChatRepository(client, database, userRepo, snowflake, cache);
            });
            services.AddSingleton<IActivityLogService>(sp =>
            {
                var db = sp.GetRequiredService<IMongoDatabase>();
                var logger = sp.GetRequiredService<ILogger<ActivityLogService>>();
                return new ActivityLogService(db, logger);
            });
        }
        else
        {
            services.AddScoped<IChatRepository, ChatRepository>();
        }

        // ========== RABBITMQ MESSAGE BROKER ==========
        var rabbitSection = configuration.GetSection(RabbitMqSettings.SectionName);
        var rabbitEnabled = rabbitSection.GetValue<bool>("Enabled");
        if (rabbitEnabled)
        {
            services.Configure<RabbitMqSettings>(rabbitSection);
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
            services.AddHostedService(sp => new ChatMessageConsumerService(
                sp.GetRequiredService<IConnection>(),
                sp,
                sp.GetRequiredService<ILogger<ChatMessageConsumerService>>(),
                sp.GetService<IConnectionMultiplexer>()));
            services.AddHostedService(sp => new LikeNotificationConsumer(
                sp.GetRequiredService<IConnection>(),
                sp,
                sp.GetRequiredService<ILogger<LikeNotificationConsumer>>(),
                sp.GetService<IConnectionMultiplexer>()));
            services.AddHostedService<SocialTechsy.SocialNetwork.Infrastructure.MongoDB.OutboxProcessor>();
            services.AddHostedService(sp => new SqlOutboxProcessor(
                sp,
                sp.GetRequiredService<IConnection>(),
                sp.GetRequiredService<ILogger<SqlOutboxProcessor>>()));
        }

        // Social Networking repositories
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<IFollowRepository, FollowRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ITagPreferenceRepository, TagPreferenceRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<DataSeeder>();
        services.AddSingleton<ICacheService>(sp =>
            new CacheService(
                sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                sp.GetService<IConnectionMultiplexer>()));

        // Resilience pipelines (Polly retry + circuit breaker)
        services.AddResiliencePipelines();

        // Gitea integration
        services.Configure<GiteaConfiguration>(
            configuration.GetSection(GiteaConfiguration.SectionName));
        services.AddHttpClient<IGiteaService, GiteaService>();

        return services;
    }
}
