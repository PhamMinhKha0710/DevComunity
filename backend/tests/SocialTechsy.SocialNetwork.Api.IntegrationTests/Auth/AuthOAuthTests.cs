using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Auth;

public class AuthOAuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthOAuthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Exchange_ValidCode_Returns200WithTokens_Tc030()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SocialTechsySocialNetworkDbContext>();
        var user = User.Create("oauthuser", "oauth@test.local", "x", "OAuth User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var uid = user.UserId;
        var code = "oauth-code-" + Guid.NewGuid().ToString("N");
        var session = new OAuthLoginSession
        {
            Code = code,
            UserId = uid,
            Provider = "google",
            AccessToken = "at-test",
            RefreshToken = "rt-test",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(4),
            IsUsed = false
        };
        await db.OAuthLoginSessions.AddAsync(session);
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/exchange", new { code }, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("user").GetProperty("userId").GetInt32().Should().Be(uid);
    }

    [Fact]
    public async Task Exchange_InvalidOrExpiredCode_Returns400_Tc032()
    {
        // 1. Fake code
        var response1 = await _client.PostAsJsonAsync("/api/auth/exchange", new { code = "fake-code" }, JsonOptions.CamelCase);
        response1.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 2. Used code
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SocialTechsySocialNetworkDbContext>();
        var user = User.Create("oauthuser2", "oauth2@test.local", "x", "OAuth User 2");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var code = "used-code-" + Guid.NewGuid().ToString("N");
        var session = new OAuthLoginSession
        {
            Code = code,
            UserId = user.UserId,
            Provider = "google",
            AccessToken = "at-test",
            RefreshToken = "rt-test",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(4),
            IsUsed = true // ALREADY USED
        };
        await db.OAuthLoginSessions.AddAsync(session);
        await db.SaveChangesAsync();

        var response2 = await _client.PostAsJsonAsync("/api/auth/exchange", new { code }, JsonOptions.CamelCase);
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response2.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("message").GetString().Should().Contain("used");
    }

    [Fact]
    public async Task Callback_ExistingEmail_LinksAccount_Tc031()
    {
        // 1. Create a user with email/pass
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SocialTechsySocialNetworkDbContext>();
        var email = "existing@test.local";
        var user = User.Create("existinguser", email, "password-hash", "Existing User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        db.Entry(user).State = EntityState.Detached; // DETACH to avoid tracking conflicts in service

        var externalAuthService = scope.ServiceProvider.GetRequiredService<IExternalAuthService>();

        // 2. Simulate OAuth callback with same email
        var result = await externalAuthService.ProcessCallbackAsync(
            "google", "google-id-123", email, "Google User", "http://avatar.url");

        // 3. Verify success and linking
        result.IsSuccess.Should().BeTrue();
        result.UserId.Should().Be(user.UserId); // SAME USER ID
        
        var updatedUser = await db.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        
        updatedUser.Should().NotBeNull();
        updatedUser!.ExternalProvider.Should().Be("google");
        updatedUser!.ExternalProviderId.Should().Be("google-id-123");
    }
}
