using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Security;

[Trait("Category", "Security")]
public class SecurityIdorTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SecurityIdorTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Tc_Sc008_PrivatePost_GetById_Stranger_CurrentBehavior_DocumentsGap()
    {
        var (clientA, _) = await SocialTestHelper.RegisterUserAsync(_factory, "idorA" + Guid.NewGuid().ToString("N")[..6]);
        var (clientB, _) = await SocialTestHelper.RegisterUserAsync(_factory, "idorB" + Guid.NewGuid().ToString("N")[..6]);

        var created = await SocialTestHelper.CreatePostAsync(clientA, "PRIVATE_IDOR_TC8", "private");
        var postId = created.GetProperty("postId").GetInt32();

        var response = await clientB.GetAsync($"/api/newsfeed/posts/{postId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "GetPostQueryHandler không chặn bài private khi không thuộc nhóm — GAP so với kỳ vọng IDOR 403/404 (xem SECURITY_TEST_REPORT)");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("content").GetString().Should().Be("PRIVATE_IDOR_TC8");
    }
}
