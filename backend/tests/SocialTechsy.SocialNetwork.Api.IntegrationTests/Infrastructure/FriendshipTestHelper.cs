using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

internal static class FriendshipTestHelper
{
    public static async Task EnsureAcceptedFriendshipAsync(CustomWebApplicationFactory factory, int userIdA, int userIdB)
    {
        if (userIdA == userIdB) return;

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SocialTechsySocialNetworkDbContext>();

        var existing = await db.Friendships.FirstOrDefaultAsync(f =>
            (f.RequesterId == userIdA && f.AddresseeId == userIdB) ||
            (f.RequesterId == userIdB && f.AddresseeId == userIdA));

        if (existing != null)
        {
            if (existing.Status == FriendshipStatus.Accepted) return;
            if (existing.Status == FriendshipStatus.Pending)
            {
                existing.Accept();
                await db.SaveChangesAsync();
            }
            return;
        }

        var f = Friendship.Create(userIdA, userIdB);
        f.Accept();
        db.Friendships.Add(f);
        await db.SaveChangesAsync();
    }
}
