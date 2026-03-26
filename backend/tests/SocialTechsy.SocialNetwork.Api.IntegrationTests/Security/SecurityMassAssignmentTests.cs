using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Security;

[Trait("Category", "Security")]
public class SecurityMassAssignmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityMassAssignmentTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Tc_Sc011_Register_WithExtraIsAdmin_Ignored_UserIsNormal()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var email = $"{id}@mass.local";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new Dictionary<string, object?>
            {
                ["username"] = $"u{id}",
                ["email"] = email,
                ["password"] = "secret12",
                ["confirmPassword"] = "secret12",
                ["isAdmin"] = true,
                ["role"] = "Admin"
            },
            JsonOptions.CamelCase);
        response.EnsureSuccessStatusCode();
        var regJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = regJson.GetProperty("accessToken").GetString()!;
        var authed = TestAuthHelper.CreateAuthenticatedClient(_factory, token);

        var me = await authed.GetAsync("/api/auth/me");
        me.EnsureSuccessStatusCode();
        var meJson = await me.Content.ReadFromJsonAsync<JsonElement>();
        meJson.GetProperty("email").GetString().Should().Be(email);
        meJson.TryGetProperty("isAdmin", out _).Should().BeFalse();
        meJson.TryGetProperty("role", out _).Should().BeFalse();
    }
}
