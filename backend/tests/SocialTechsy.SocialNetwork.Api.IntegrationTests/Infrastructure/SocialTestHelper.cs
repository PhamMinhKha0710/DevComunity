using System.Net.Http.Json;
using System.Text.Json;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

internal static class SocialTestHelper
{
    public static async Task<(HttpClient Client, int UserId)> RegisterUserAsync(
        CustomWebApplicationFactory factory,
        string? suffix = null)
    {
        var client = factory.CreateClient();
        var id = suffix ?? Guid.NewGuid().ToString("N")[..8];
        var email = $"{id}@social.local";
        var res = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"u{id}",
            email,
            password = "secret12",
            confirmPassword = "secret12"
        }, JsonOptions.CamelCase);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("accessToken").GetString()!;
        var authed = TestAuthHelper.CreateAuthenticatedClient(factory, token);
        var userId = await GetUserIdAsync(authed);
        return (authed, userId);
    }

    public static async Task<int> GetUserIdAsync(HttpClient client)
    {
        var me = await client.GetAsync("/api/auth/me");
        me.EnsureSuccessStatusCode();
        var meJson = await me.Content.ReadFromJsonAsync<JsonElement>();
        return meJson.GetProperty("userId").GetInt32();
    }

    public static async Task<JsonElement> CreatePostAsync(
        HttpClient client,
        string content,
        string visibility,
        int? groupId = null)
    {
        var res = await client.PostAsJsonAsync("/api/newsfeed/posts", new
        {
            content,
            visibility,
            groupId
        }, JsonOptions.CamelCase);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }
}
