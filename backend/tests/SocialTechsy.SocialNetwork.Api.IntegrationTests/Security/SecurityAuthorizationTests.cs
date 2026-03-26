using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Security;

[Trait("Category", "Security")]
public class SecurityAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Tc_Sc016_CreateQuestion_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/questions", new
        {
            title = "t",
            body = "b",
            tags = Array.Empty<string>()
        }, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tc_Sc016_Logout_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
