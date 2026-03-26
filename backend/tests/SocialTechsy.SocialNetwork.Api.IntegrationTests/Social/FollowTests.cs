using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Social;

public class FollowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FollowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Follow_Unfollow_Stats_TcS040_TcS041_TcS042()
    {
        var (clientA, idA) = await SocialTestHelper.RegisterUserAsync(_factory, "foa" + Guid.NewGuid().ToString("N")[..6]);
        var (clientB, idB) = await SocialTestHelper.RegisterUserAsync(_factory, "fob" + Guid.NewGuid().ToString("N")[..6]);
        var (clientC, idC) = await SocialTestHelper.RegisterUserAsync(_factory, "foc" + Guid.NewGuid().ToString("N")[..6]);

        (await clientB.PostAsync($"/api/follow/{idA}", null)).EnsureSuccessStatusCode();
        (await clientC.PostAsync($"/api/follow/{idA}", null)).EnsureSuccessStatusCode();

        var stats = await clientA.GetAsync($"/api/follow/stats/{idA}");
        stats.EnsureSuccessStatusCode();
        var s = await stats.Content.ReadFromJsonAsync<JsonElement>();
        s.GetProperty("followersCount").GetInt32().Should().BeGreaterThanOrEqualTo(2);

        (await clientB.DeleteAsync($"/api/follow/{idA}")).EnsureSuccessStatusCode();

        var stats2 = await clientA.GetAsync($"/api/follow/stats/{idA}");
        stats2.EnsureSuccessStatusCode();
        var s2 = await stats2.Content.ReadFromJsonAsync<JsonElement>();
        s2.GetProperty("followersCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SelfFollow_Returns400_TcS043()
    {
        var (client, id) = await SocialTestHelper.RegisterUserAsync(_factory, "sf" + Guid.NewGuid().ToString("N")[..6]);
        var r = await client.PostAsync($"/api/follow/{id}", null);
        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
