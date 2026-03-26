using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Social;

public class GroupsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GroupsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task CreateGroup_Join_Post_Leave_TcS020_TcS021_TcS022_TcS024()
    {
        var (clientA, idA) = await SocialTestHelper.RegisterUserAsync(_factory, "ga" + Guid.NewGuid().ToString("N")[..6]);
        var (clientB, idB) = await SocialTestHelper.RegisterUserAsync(_factory, "gb" + Guid.NewGuid().ToString("N")[..6]);

        var create = await clientA.PostAsJsonAsync("/api/groups", new
        {
            name = "React VN " + Guid.NewGuid().ToString("N")[..8],
            description = "desc",
            isPrivate = false
        }, JsonOptions.CamelCase);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var g = await create.Content.ReadFromJsonAsync<JsonElement>();
        var groupId = g.GetProperty("groupId").GetInt32();

        var join = await clientB.PostAsync($"/api/groups/{groupId}/join", null);
        join.StatusCode.Should().Be(HttpStatusCode.OK);

        var postRes = await SocialTestHelper.CreatePostAsync(clientB, "IN_GROUP_POST", "public", groupId);
        postRes.GetProperty("groupId").GetInt32().Should().Be(groupId);

        var groupFeed = await clientB.GetAsync($"/api/newsfeed/groups/{groupId}?page=1&pageSize=10");
        groupFeed.EnsureSuccessStatusCode();
        var gf = await groupFeed.Content.ReadFromJsonAsync<JsonElement>();
        gf.GetProperty("items").EnumerateArray().Any(e => e.GetProperty("content").GetString() == "IN_GROUP_POST").Should().BeTrue();

        var leave = await clientB.PostAsync($"/api/groups/{groupId}/leave", null);
        leave.StatusCode.Should().Be(HttpStatusCode.OK);

        var my = await clientB.GetAsync("/api/groups/my");
        my.EnsureSuccessStatusCode();
        var myJson = await my.Content.ReadFromJsonAsync<JsonElement>();
        if (myJson.ValueKind == JsonValueKind.Array)
        {
            myJson.EnumerateArray().Select(e => e.GetProperty("groupId").GetInt32()).Should().NotContain(groupId);
        }
    }

    [Fact]
    public async Task PostToGroupNonMember_Returns403_TcS023()
    {
        var (clientA, _) = await SocialTestHelper.RegisterUserAsync(_factory, "g1a" + Guid.NewGuid().ToString("N")[..6]);
        var (clientC, _) = await SocialTestHelper.RegisterUserAsync(_factory, "g1c" + Guid.NewGuid().ToString("N")[..6]);

        var create = await clientA.PostAsJsonAsync("/api/groups", new
        {
            name = "Priv " + Guid.NewGuid().ToString("N")[..8],
            description = "d",
            isPrivate = false
        }, JsonOptions.CamelCase);
        create.EnsureSuccessStatusCode();
        var groupId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("groupId").GetInt32();

        var res = await clientC.PostAsJsonAsync("/api/newsfeed/posts", new
        {
            content = "hack",
            visibility = "public",
            groupId
        }, JsonOptions.CamelCase);
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
