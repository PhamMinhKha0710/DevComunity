using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Security;

[Trait("Category", "Security")]
public class SecurityJwtValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityJwtValidationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Tc_Sc001_Me_WithTamperedJwtSignature_Returns401()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var reg = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"u{id}",
            email = $"{id}@jwt.local",
            password = "secret12",
            confirmPassword = "secret12"
        }, JsonOptions.CamelCase);
        reg.EnsureSuccessStatusCode();
        var regJson = await reg.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = regJson.GetProperty("accessToken").GetString()!;
        var bad = SecurityJwtHelper.TamperSignature(accessToken);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bad);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tc_Sc002_Me_WithMalformedJwt_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.jwt");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tc_Sc003_Me_WithExpiredJwt_Returns401()
    {
        var expired = SecurityJwtHelper.CreateExpiredAccessToken();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expired);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
