using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Social;

public class SocialLikeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SocialLikeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task LikePost_ToggleRedis_TcS007()
    {
        var (author, _) = await SocialTestHelper.RegisterUserAsync(_factory, "lk1" + Guid.NewGuid().ToString("N")[..6]);
        var (liker, _) = await SocialTestHelper.RegisterUserAsync(_factory, "lk2" + Guid.NewGuid().ToString("N")[..6]);

        var post = await SocialTestHelper.CreatePostAsync(author, "like me", "public");
        var postId = post.GetProperty("postId").GetInt32();

        var like1 = await liker.PostAsync($"/api/newsfeed/posts/{postId}/like", null);
        like1.StatusCode.Should().Be(HttpStatusCode.OK);
        var j1 = await like1.Content.ReadFromJsonAsync<JsonElement>();
        j1.GetProperty("userLiked").GetBoolean().Should().BeTrue();

        var unlike = await liker.DeleteAsync($"/api/newsfeed/posts/{postId}/like");
        unlike.StatusCode.Should().Be(HttpStatusCode.OK);
        var j2 = await unlike.Content.ReadFromJsonAsync<JsonElement>();
        j2.GetProperty("userLiked").GetBoolean().Should().BeFalse();
    }
}
