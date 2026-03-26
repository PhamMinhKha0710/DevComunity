using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Security;

[Trait("Category", "Security")]
public class SecurityXssApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SecurityXssApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Tc_Sc006_Tc_Sc007_PostWithScriptTag_JsonReturnsRawString_ManualXssInBrowser()
    {
        const string payload = "<script>alert(1)</script>";
        var (client, _) = await SocialTestHelper.RegisterUserAsync(_factory, "xss" + Guid.NewGuid().ToString("N")[..6]);
        var created = await SocialTestHelper.CreatePostAsync(client, payload, "public");
        var postId = created.GetProperty("postId").GetInt32();

        var get = await client.GetAsync($"/api/newsfeed/posts/{postId}");
        get.EnsureSuccessStatusCode();
        var json = await get.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("content").GetString().Should().Be(payload);
    }
}
