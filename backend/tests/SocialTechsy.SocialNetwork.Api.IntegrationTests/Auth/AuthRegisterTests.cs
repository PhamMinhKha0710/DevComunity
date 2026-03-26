using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Auth;

public class AuthRegisterTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthRegisterTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Register_Valid_Returns200WithoutTokens_Tc001()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var body = new
        {
            username = $"u{id}",
            email = $"{id}@test.local",
            password = "secret12",
            confirmPassword = "secret12",
            displayName = "Test User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("accessToken").GetString().Should().BeNull();
        json.GetProperty("refreshToken").GetString().Should().BeNull();
        json.GetProperty("user").GetProperty("username").GetString().Should().Be($"u{id}");
        json.GetProperty("message").GetString().Should().Contain("log in");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400_Tc002()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var email = $"{id}@dup.local";
        var first = new
        {
            username = $"a{id}",
            email,
            password = "secret12",
            confirmPassword = "secret12"
        };
        var second = new
        {
            username = $"b{id}",
            email,
            password = "secret12",
            confirmPassword = "secret12"
        };

        (await _client.PostAsJsonAsync("/api/auth/register", first, JsonOptions.CamelCase)).StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await _client.PostAsJsonAsync("/api/auth/register", second, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("message").GetString().Should().Be("Email already in use");
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns400_Tc003()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var username = $"same{id}";
        var first = new
        {
            username,
            email = $"{id}1@test.local",
            password = "secret12",
            confirmPassword = "secret12"
        };
        var second = new
        {
            username,
            email = $"{id}2@test.local",
            password = "secret12",
            confirmPassword = "secret12"
        };

        (await _client.PostAsJsonAsync("/api/auth/register", first, JsonOptions.CamelCase)).StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await _client.PostAsJsonAsync("/api/auth/register", second, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("message").GetString().Should().Be("Username already taken");
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400_Tc004()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var body = new
        {
            username = $"u{id}",
            email = $"{id}@test.local",
            password = "123",
            confirmPassword = "123"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var text = await response.Content.ReadAsStringAsync();
        text.Should().Contain("Validation");
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400_Tc005()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var body = new
        {
            username = $"u{id}",
            email = "abc@",
            password = "secret12",
            confirmPassword = "secret12"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var text = await response.Content.ReadAsStringAsync();
        text.Should().Contain("Validation");
    }

    [Fact]
    public async Task Register_EmptyPayload_Returns400_Tc006()
    {
        var body = new { };

        var response = await _client.PostAsJsonAsync("/api/auth/register", body, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var text = await response.Content.ReadAsStringAsync();
        text.Should().Contain("errors"); // ProblemDetails usually has "errors"
    }
}
