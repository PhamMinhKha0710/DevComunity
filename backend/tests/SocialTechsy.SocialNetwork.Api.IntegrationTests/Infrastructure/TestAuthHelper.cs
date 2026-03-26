using System.Net.Http.Json;
using System.Text.Json;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

internal static class TestAuthHelper
{
    public static async Task<string> RegisterAndGetTokenAsync(HttpClient client, string? suffix = null)
    {
        var id = suffix ?? Guid.NewGuid().ToString("N")[..8];
        var email = $"{id}@qa.local";
        var username = $"u{id}";
        var password = "secret12";

        var regRes = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email,
            password,
            confirmPassword = password
        }, JsonOptions.CamelCase);
        regRes.EnsureSuccessStatusCode();

        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password,
            rememberMe = false
        }, JsonOptions.CamelCase);
        loginRes.EnsureSuccessStatusCode();
        var json = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("accessToken").GetString()!;
    }

    public static HttpClient CreateAuthenticatedClient(CustomWebApplicationFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
