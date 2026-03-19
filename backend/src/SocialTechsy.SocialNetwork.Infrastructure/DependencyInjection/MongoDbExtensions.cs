using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.GridFs;
using SocialTechsy.SocialNetwork.Infrastructure.IdGeneration;
using SocialTechsy.SocialNetwork.Infrastructure.MongoDB;
using RedisChatCacheService = SocialTechsy.SocialNetwork.Infrastructure.Caching.RedisChatCacheService;

namespace SocialTechsy.SocialNetwork.Infrastructure.DependencyInjection;

public static class MongoDbExtensions
{
    public static IServiceCollection AddMongoDb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mongoSection = configuration.GetSection(MongoDbSettings.SectionName);
        if (!mongoSection.Exists() || string.IsNullOrEmpty(mongoSection["ConnectionString"]))
        {
            services.AddScoped<IChatRepository, MongoChatRepository>();
            return services;
        }

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
        services.AddSingleton<IGridFsService, GridFsService>();

        return services;
    }
}
