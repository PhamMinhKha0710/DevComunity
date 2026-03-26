using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Security;

[Trait("Category", "Security")]
public class SecuritySearchAndLoginTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecuritySearchAndLoginTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Theory]
    [InlineData("' OR 1=1--")]
    [InlineData("'; DROP TABLE Users;--")]
    [InlineData("1' UNION SELECT null--")]
    public async Task Tc_Sc004_Search_SqliPayloads_Return200WithoutServerError(string q)
    {
        var response = await _client.GetAsync($"/api/search?q={Uri.EscapeDataString(q)}&maxResults=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = (int)response.StatusCode;
        status.Should().BeLessThan(500);
    }

    [Fact]
    public async Task Tc_Sc005_Login_WithSqlInjectionLikeEmail_DoesNotSucceed()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin'--",
            password = "anything"
        }, JsonOptions.CamelCase);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }
}
