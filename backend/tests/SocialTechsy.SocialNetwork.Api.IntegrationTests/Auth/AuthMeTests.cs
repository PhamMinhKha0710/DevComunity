using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Auth;

public class AuthMeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthMeTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Me_WithBearerToken_Returns200_Tc015()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var email = $"{id}@me.local";
        var reg = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"u{id}",
            email,
            password = "secret12",
            confirmPassword = "secret12"
        }, JsonOptions.CamelCase);
        var regJson = await reg.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = regJson.GetProperty("accessToken").GetString()!;

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("email").GetString().Should().Be(email);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401_Tc016()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
