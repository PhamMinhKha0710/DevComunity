using System.Net;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Security;

[Trait("Category", "Security")]
public class SecurityHeadersTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityHeadersTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Tc_Sc013_QuestionsList_IncludesSecurityHeaders()
    {
        var response = await _client.GetAsync("/api/questions?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
        response.Headers.Contains("Referrer-Policy").Should().BeTrue();
        response.Headers.Contains("X-XSS-Protection").Should().BeTrue();
        response.Headers.Contains("Content-Security-Policy").Should().BeTrue();
        response.Headers.Contains("Strict-Transport-Security").Should().BeFalse();
    }

}
