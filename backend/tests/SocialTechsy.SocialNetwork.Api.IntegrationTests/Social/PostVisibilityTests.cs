using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Social;

public class PostVisibilityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PostVisibilityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task CreatePost_Public_Returns201_TcS002()
    {
        var (client, _) = await SocialTestHelper.RegisterUserAsync(_factory);
        var res = await SocialTestHelper.CreatePostAsync(client, "Hello public #tc2", "public");
        res.GetProperty("postId").GetInt32().Should().BeGreaterThan(0);
        res.GetProperty("visibility").GetString().Should().Be("public");
    }

    [Fact]
    public async Task FriendsOnlyPost_FriendAndFollowerSeeOnDefaultFeed_TcS003()
    {
        var (clientA, idA) = await SocialTestHelper.RegisterUserAsync(_factory, "v3a" + Guid.NewGuid().ToString("N")[..6]);
        var (clientB, idB) = await SocialTestHelper.RegisterUserAsync(_factory, "v3b" + Guid.NewGuid().ToString("N")[..6]);
        var (clientC, _) = await SocialTestHelper.RegisterUserAsync(_factory, "v3c" + Guid.NewGuid().ToString("N")[..6]);

        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory, idA, idB);
        (await clientC.PostAsync($"/api/follow/{idA}", null)).EnsureSuccessStatusCode();

        await SocialTestHelper.CreatePostAsync(clientA, "FRIENDS_ONLY_TC3", "friends");

        var feedB = await clientB.GetAsync("/api/newsfeed?page=1&pageSize=20");
        var bItems = await ReadContentsAsync(feedB);
        bItems.Should().Contain("FRIENDS_ONLY_TC3");

        var feedC = await clientC.GetAsync("/api/newsfeed?page=1&pageSize=20");
        var cItems = await ReadContentsAsync(feedC);
        cItems.Should().Contain("FRIENDS_ONLY_TC3");
    }

    [Fact]
    public async Task FollowingVisibility_FollowerSees_TcS004()
    {
        var (clientA, idA) = await SocialTestHelper.RegisterUserAsync(_factory, "v4a" + Guid.NewGuid().ToString("N")[..6]);
        var (clientB, idB) = await SocialTestHelper.RegisterUserAsync(_factory, "v4b" + Guid.NewGuid().ToString("N")[..6]);
        var (clientC, idC) = await SocialTestHelper.RegisterUserAsync(_factory, "v4c" + Guid.NewGuid().ToString("N")[..6]);

        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory, idA, idB);
        (await clientC.PostAsync($"/api/follow/{idA}", null)).EnsureSuccessStatusCode();

        await SocialTestHelper.CreatePostAsync(clientA, "FOLLOWING_VIS_TC4", "following");

        var feedB = await clientB.GetAsync("/api/newsfeed?page=1&pageSize=20");
        (await ReadContentsAsync(feedB)).Should().Contain("FOLLOWING_VIS_TC4");

        var feedC = await clientC.GetAsync("/api/newsfeed?page=1&pageSize=20");
        (await ReadContentsAsync(feedC)).Should().Contain("FOLLOWING_VIS_TC4");
    }

    [Fact]
    public async Task PrivatePost_OnlyAuthorSeesOnFeed_TcS005()
    {
        var (clientA, _) = await SocialTestHelper.RegisterUserAsync(_factory, "v5a" + Guid.NewGuid().ToString("N")[..6]);
        var (clientB, _) = await SocialTestHelper.RegisterUserAsync(_factory, "v5b" + Guid.NewGuid().ToString("N")[..6]);

        await SocialTestHelper.CreatePostAsync(clientA, "PRIVATE_TC5", "private");

        var feedA = await clientA.GetAsync("/api/newsfeed?page=1&pageSize=20");
        (await ReadContentsAsync(feedA)).Should().Contain("PRIVATE_TC5");

        var feedB = await clientB.GetAsync("/api/newsfeed?page=1&pageSize=20");
        (await ReadContentsAsync(feedB)).Should().NotContain("PRIVATE_TC5");
    }

    [Fact]
    public async Task UpdateAndDeletePost_Author_TcS010()
    {
        var (client, _) = await SocialTestHelper.RegisterUserAsync(_factory);
        var created = await SocialTestHelper.CreatePostAsync(client, "before edit", "public");
        var postId = created.GetProperty("postId").GetInt32();

        var put = await client.PutAsJsonAsync($"/api/newsfeed/posts/{postId}", new { content = "after edit" }, JsonOptions.CamelCase);
        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await put.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("content").GetString().Should().Be("after edit");

        var del = await client.DeleteAsync($"/api/newsfeed/posts/{postId}");
        del.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await client.GetAsync($"/api/newsfeed/posts/{postId}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<List<string>> ReadContentsAsync(HttpResponseMessage r)
    {
        r.EnsureSuccessStatusCode();
        var doc = await r.Content.ReadFromJsonAsync<JsonElement>();
        return doc.GetProperty("items").EnumerateArray().Select(e => e.GetProperty("content").GetString()!).ToList();
    }
}
