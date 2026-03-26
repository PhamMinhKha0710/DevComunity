using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Social;

public class FriendshipFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FriendshipFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task SendFriendRequest_Accept_TcS030_TcS031()
    {
        var (clientA, idA) = await SocialTestHelper.RegisterUserAsync(_factory, "f1a" + Guid.NewGuid().ToString("N")[..6]);
        var (clientB, idB) = await SocialTestHelper.RegisterUserAsync(_factory, "f1b" + Guid.NewGuid().ToString("N")[..6]);

        var req = await clientA.PostAsync($"/api/friendship/request/{idB}", null);
        req.StatusCode.Should().Be(HttpStatusCode.Created);
        var fr = await req.Content.ReadFromJsonAsync<JsonElement>();
        var friendshipId = fr.GetProperty("friendshipId").GetInt32();

        var acc = await clientB.PutAsync($"/api/friendship/accept/{friendshipId}", null);
        acc.StatusCode.Should().Be(HttpStatusCode.OK);
        var accepted = await acc.Content.ReadFromJsonAsync<JsonElement>();
        accepted.GetProperty("status").GetString().Should().Be("Accepted");
    }

    [Fact]
    public async Task SendFriendRequest_Reject_TcS032()
    {
        var (clientA, idA) = await SocialTestHelper.RegisterUserAsync(_factory, "f2a" + Guid.NewGuid().ToString("N")[..6]);
        var (clientB, idB) = await SocialTestHelper.RegisterUserAsync(_factory, "f2b" + Guid.NewGuid().ToString("N")[..6]);

        var req = await clientA.PostAsync($"/api/friendship/request/{idB}", null);
        req.EnsureSuccessStatusCode();
        var friendshipId = (await req.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("friendshipId").GetInt32();

        var rej = await clientB.PutAsync($"/api/friendship/reject/{friendshipId}", null);
        rej.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DuplicateFriendRequest_Returns400_TcS033()
    {
        var (clientA, idA) = await SocialTestHelper.RegisterUserAsync(_factory, "f3a" + Guid.NewGuid().ToString("N")[..6]);
        var (clientB, idB) = await SocialTestHelper.RegisterUserAsync(_factory, "f3b" + Guid.NewGuid().ToString("N")[..6]);

        (await clientA.PostAsync($"/api/friendship/request/{idB}", null)).EnsureSuccessStatusCode();
        var second = await clientA.PostAsync($"/api/friendship/request/{idB}", null);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FriendSuggestions_Returns200_TcS034()
    {
        var (clientA, _) = await SocialTestHelper.RegisterUserAsync(_factory, "s4a" + Guid.NewGuid().ToString("N")[..6]);

        var r = await clientA.GetAsync("/api/friendship/suggestions?limit=20");
        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await r.Content.ReadFromJsonAsync<JsonElement>();
        arr.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
