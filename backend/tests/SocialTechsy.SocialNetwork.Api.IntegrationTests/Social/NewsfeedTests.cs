using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Social;

public class NewsfeedTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NewsfeedTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task GetNewsfeed_AggregatesFriendsFollowsGroups_TcS001()
    {
        var (clientA, idA) = await SocialTestHelper.RegisterUserAsync(_factory, "a" + Guid.NewGuid().ToString("N")[..6]);
        var (clientB, idB) = await SocialTestHelper.RegisterUserAsync(_factory, "b" + Guid.NewGuid().ToString("N")[..6]);
        var (clientC, idC) = await SocialTestHelper.RegisterUserAsync(_factory, "c" + Guid.NewGuid().ToString("N")[..6]);
        var (clientD, _) = await SocialTestHelper.RegisterUserAsync(_factory, "d" + Guid.NewGuid().ToString("N")[..6]);

        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory, idA, idB);

        (await clientA.PostAsync($"/api/follow/{idC}", null)).EnsureSuccessStatusCode();

        var groupRes = await clientA.PostAsJsonAsync("/api/groups", new
        {
            name = "G Social TC001 " + Guid.NewGuid().ToString("N")[..6],
            description = "test",
            isPrivate = false
        }, JsonOptions.CamelCase);
        groupRes.EnsureSuccessStatusCode();
        var groupJson = await groupRes.Content.ReadFromJsonAsync<JsonElement>();
        var groupId = groupJson.GetProperty("groupId").GetInt32();

        (await clientB.PostAsync($"/api/groups/{groupId}/join", null)).EnsureSuccessStatusCode();

        await SocialTestHelper.CreatePostAsync(clientB, "POST_B_PUBLIC_TC001", "public");
        await SocialTestHelper.CreatePostAsync(clientB, "POST_B_PRIVATE_TC001", "private");
        await SocialTestHelper.CreatePostAsync(clientC, "POST_C_PUBLIC_TC001", "public");
        await SocialTestHelper.CreatePostAsync(clientD, "POST_D_STRANGER_TC001", "public");
        await SocialTestHelper.CreatePostAsync(clientB, "POST_GROUP_TC001", "public", groupId);

        var feed = await clientA.GetAsync("/api/newsfeed?page=1&pageSize=50");
        feed.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await feed.Content.ReadFromJsonAsync<JsonElement>();
        var items = doc.GetProperty("items");
        var contents = items.EnumerateArray().Select(e => e.GetProperty("content").GetString()).ToHashSet();

        contents.Should().Contain("POST_B_PUBLIC_TC001");
        contents.Should().Contain("POST_C_PUBLIC_TC001");
        contents.Should().Contain("POST_GROUP_TC001");
        contents.Should().Contain("POST_D_STRANGER_TC001");

        contents.Should().Contain("POST_B_PRIVATE_TC001");
    }

    [Fact]
    public async Task GetNewsfeed_Filters_Following_Friends_Groups_TcS060()
    {
        var (clientA, idA) = await SocialTestHelper.RegisterUserAsync(_factory, "fa" + Guid.NewGuid().ToString("N")[..6]);
        var (clientB, idB) = await SocialTestHelper.RegisterUserAsync(_factory, "fb" + Guid.NewGuid().ToString("N")[..6]);
        var (clientC, idC) = await SocialTestHelper.RegisterUserAsync(_factory, "fc" + Guid.NewGuid().ToString("N")[..6]);

        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory, idA, idB);
        (await clientA.PostAsync($"/api/follow/{idC}", null)).EnsureSuccessStatusCode();

        await SocialTestHelper.CreatePostAsync(clientB, "ONLY_FRIEND_TC060", "public");
        await SocialTestHelper.CreatePostAsync(clientC, "ONLY_FOLLOW_TC060", "public");

        var groupRes = await clientA.PostAsJsonAsync("/api/groups", new
        {
            name = "G Filter " + Guid.NewGuid().ToString("N")[..6],
            description = "x",
            isPrivate = false
        }, JsonOptions.CamelCase);
        groupRes.EnsureSuccessStatusCode();
        var gid = (await groupRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("groupId").GetInt32();
        (await clientA.PostAsync($"/api/groups/{gid}/join", null)).EnsureSuccessStatusCode();
        await SocialTestHelper.CreatePostAsync(clientA, "ONLY_GROUP_TC060", "public", gid);

        async Task<string[]> Contents(string? filter)
        {
            var url = filter == null ? "/api/newsfeed?page=1&pageSize=50" : $"/api/newsfeed?filter={filter}&page=1&pageSize=50";
            var r = await clientA.GetAsync(url);
            r.EnsureSuccessStatusCode();
            var j = await r.Content.ReadFromJsonAsync<JsonElement>();
            return j.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("content").GetString()!).ToArray();
        }

        var friends = await Contents("friends");
        friends.Should().Contain("ONLY_FRIEND_TC060");
        friends.Should().NotContain("ONLY_FOLLOW_TC060");

        var following = await Contents("following");
        following.Should().Contain("ONLY_FOLLOW_TC060");
        following.Should().NotContain("ONLY_FRIEND_TC060");

        var groups = await Contents("groups");
        groups.Should().Contain("ONLY_GROUP_TC060");
    }
}
