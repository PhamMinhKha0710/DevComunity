using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

/// <summary>
/// Factory for creating DbContext instances at design time (e.g. for EF Core CLI migrations).
/// This allows 'dotnet ef' commands to work without the full application host.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SocialTechsySocialNetworkDbContext>
{
    public SocialTechsySocialNetworkDbContext CreateDbContext(string[] args)
    {
        // Build configuration from the API project's appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "SocialTechsy.SocialNetwork.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<SocialTechsySocialNetworkDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        optionsBuilder.UseSqlServer(
            connectionString,
            b => b.MigrationsAssembly(typeof(SocialTechsySocialNetworkDbContext).Assembly.FullName));

        return new SocialTechsySocialNetworkDbContext(optionsBuilder.Options);
    }
}
