using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

public static class FullTextSearchSetup
{
    /// <summary>
    /// Creates Full-Text catalog and index on Questions(Title, Body) if not already present.
    /// Requires SQL Server Full-Text Search feature to be installed.
    /// </summary>
    public static async Task EnsureFullTextIndexAsync(
        SocialTechsySocialNetworkDbContext context,
        ILogger? logger = null)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'SocialTechsyFTCatalog')
                    CREATE FULLTEXT CATALOG SocialTechsyFTCatalog AS DEFAULT;
            ");

            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.fulltext_indexes fi
                    JOIN sys.tables t ON fi.object_id = t.object_id
                    WHERE t.name = 'Questions')
                BEGIN
                    CREATE FULLTEXT INDEX ON Questions(Title, Body)
                        KEY INDEX PK_Questions ON SocialTechsyFTCatalog
                        WITH CHANGE_TRACKING AUTO;
                END
            ");

            logger?.LogInformation("Full-text search index ensured on Questions(Title, Body)");
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex,
                "Full-text search setup skipped (feature may not be installed). " +
                "Falling back to LIKE queries. Set Search:FullTextEnabled=false in config.");
        }
    }
}
