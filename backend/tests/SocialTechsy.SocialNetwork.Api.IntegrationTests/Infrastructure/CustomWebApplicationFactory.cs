using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly object _initLock = new();
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CustomWebApplicationFactory()
    {
        lock (_initLock)
        {
            _connection.Open();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
                ["JwtSettings:Issuer"] = "SocialTechsy.SocialNetwork",
                ["JwtSettings:Audience"] = "SocialTechsy.SocialNetworkUsers",
                ["JwtSettings:ExpirationMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7",
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["Redis:Enabled"] = "true",
                ["Redis:ConnectionString"] = "localhost:6379",
                ["MongoDB:ConnectionString"] = "mongodb://127.0.0.1:27017",
                ["RabbitMQ:Enabled"] = "false",
                ["Search:FullTextEnabled"] = "false",
                ["MiniProfiler:Enabled"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            RemoveDbContextRegistrations(services);

            services.AddDbContext<SocialTechsySocialNetworkDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            var cacheDesc = services.FirstOrDefault(d => d.ServiceType == typeof(ICacheService));
            if (cacheDesc != null)
                services.Remove(cacheDesc);
            services.AddSingleton<ICacheService, NoopCacheService>();
        });
    }

    private static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        var descriptors = services.Where(d =>
            d.ServiceType == typeof(SocialTechsySocialNetworkDbContext) ||
            d.ServiceType == typeof(DbContextOptions<SocialTechsySocialNetworkDbContext>)).ToList();
        foreach (var d in descriptors)
            services.Remove(d);
    }

    public void EnsureDatabaseCreated()
    {
        lock (_initLock)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SocialTechsySocialNetworkDbContext>();
            db.Database.EnsureCreated();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _connection.Dispose();
        base.Dispose(disposing);
    }
}
