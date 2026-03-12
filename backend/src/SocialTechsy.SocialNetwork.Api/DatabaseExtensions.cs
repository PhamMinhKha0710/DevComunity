using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

namespace SocialTechsy.SocialNetwork.Api;

public static class DatabaseExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

        await seeder.SeedAsync();

        if (app.Configuration.GetValue<bool>("Search:FullTextEnabled"))
        {
            var context = scope.ServiceProvider
                .GetRequiredService<SocialTechsySocialNetworkDbContext>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILogger<SocialTechsySocialNetworkDbContext>>();
            await FullTextSearchSetup.EnsureFullTextIndexAsync(context, logger);
        }
    }
}
