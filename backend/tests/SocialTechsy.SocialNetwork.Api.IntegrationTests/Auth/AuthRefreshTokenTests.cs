using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Auth;

public class AuthRefreshTokenTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthRefreshTokenTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewPair_Tc020()
    {
        var refresh = await LoginAndGetRefreshAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh }, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Returns401_Tc021()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SocialTechsySocialNetworkDbContext>();
        var user = User.Create("expuser", "exp@t.local", "hash", "Exp");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var uid = user.UserId;
        var expired = new RefreshToken
        {
            Token = "expired-rt-token-" + Guid.NewGuid().ToString("N"),
            UserId = uid,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };
        await db.RefreshTokens.AddAsync(expired);
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = expired.Token }, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Refresh_ReuseOldToken_Returns401_Tc022()
    {
        var refresh = await LoginAndGetRefreshAsync();

        var first = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh }, JsonOptions.CamelCase);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh }, JsonOptions.CamelCase);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await second.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("message").GetString()!.ToLowerInvariant().Should().Contain("revoked");
    }

    [Fact(Skip = "SQLite + single shared connection: parallel SaveChanges throws; run against SQL Server for true race verification.")]
    public async Task Refresh_ConcurrentRequests_OnlyOneSucceeds_Tc024()
    {
        var refresh = await LoginAndGetRefreshAsync();

        var tasks = Enumerable.Range(0, 3).Select(_ =>
            _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh }, JsonOptions.CamelCase)).ToArray();

        var responses = await Task.WhenAll(tasks);

        var ok = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var unauthorized = responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized);

        ok.Should().Be(1);
        unauthorized.Should().Be(2);
    }

    private async Task<string> LoginAndGetRefreshAsync()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var email = $"{id}@refresh.local";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"u{id}",
            email,
            password = "secret12",
            confirmPassword = "secret12"
        }, JsonOptions.CamelCase);
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "secret12" }, JsonOptions.CamelCase);
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("refreshToken").GetString()!;
    }
}
