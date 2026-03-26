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

public class AuthResetPasswordTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthResetPasswordTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task ForgotPassword_EmailExists_Returns200_Tc040()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var email = $"{id}@reset.local";
        await RegisterAsync(email, $"u{id}", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email }, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPassword_EmailNotFound_Returns200_Tc041()
    {
        var email = "nonexistent@test.local";

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email }, JsonOptions.CamelCase);

        // Security requirement: Same response for existing and non-existing emails
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_ValidToken_Returns200_Tc042()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SocialTechsySocialNetworkDbContext>();
        var user = User.Create("resetuser", "reset@test.local", "oldhash", "Reset User");
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var token = "reset-token-" + Guid.NewGuid().ToString("N");
        var resetToken = new PasswordResetToken
        {
            Token = token,
            UserId = user.UserId,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };
        await db.PasswordResetTokens.AddAsync(resetToken);
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new 
        { 
            token, 
            email = user.Email,
            newPassword = "NewPassword123!",
            confirmPassword = "NewPassword123!"
        }, JsonOptions.CamelCase);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Body: {body}");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();

        // Verify token is invalidated
        var updatedToken = await db.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token);
        updatedToken.Should().NotBeNull();
        updatedToken!.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_Returns400_Tc043()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SocialTechsySocialNetworkDbContext>();
        
        var token = "expired-token-" + Guid.NewGuid().ToString("N");
        var resetToken = new PasswordResetToken
        {
            Token = token,
            UserId = 1, // Any valid or dummy ID
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        await db.PasswordResetTokens.AddAsync(resetToken);
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new 
        { 
            token, 
            email = "expired@test.local",
            newPassword = "NewPassword123!",
            confirmPassword = "NewPassword123!"
        }, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_FakeToken_Returns400_Tc044()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new 
        { 
            token = "fake-token", 
            email = "fake@test.local",
            newPassword = "NewPassword123!",
            confirmPassword = "NewPassword123!"
        }, JsonOptions.CamelCase);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        await _client.PostAsJsonAsync("/api/auth/register", body, JsonOptions.CamelCase);
    }
}
