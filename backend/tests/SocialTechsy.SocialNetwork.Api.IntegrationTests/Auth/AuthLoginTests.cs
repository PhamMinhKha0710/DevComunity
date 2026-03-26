using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Auth;

public class AuthLoginTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthLoginTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens_Tc010()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var email = $"{id}@login.local";
        var password = "secret12";
        await RegisterAsync(email, $"u{id}", password);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password }, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401_Tc011()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var email = $"{id}@login.local";
        await RegisterAsync(email, $"u{id}", "secret12");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrongpass" }, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401_Tc012()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody-exists@test.local",
            password = "secret12"
        }, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    private async Task RegisterAsync(string email, string username, string password)
    {
        var body = new
        {
            username,
            email,
            password,
            confirmPassword = password
        };
        var r = await _client.PostAsJsonAsync("/api/auth/register", body, JsonOptions.CamelCase);
        r.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
