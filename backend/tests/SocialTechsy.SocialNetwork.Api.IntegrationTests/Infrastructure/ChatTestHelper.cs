using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

internal static class ChatTestHelper
{
    public static long JsonLong(JsonElement obj, string propertyName)
    {
        var p = obj.GetProperty(propertyName);
        return JsonLongValue(p);
    }

    public static long JsonLongValue(JsonElement p) =>
        p.ValueKind == JsonValueKind.String ? long.Parse(p.GetString()!, null) : p.GetInt64();

    public static async Task<JsonElement> StartConversationAsync(HttpClient client, int recipientId, string? initialMessage = null)
    {
        var res = await client.PostAsJsonAsync("/api/chat/conversations", new
        {
            recipientId,
            initialMessage
        }, JsonOptions.CamelCase);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static async Task<JsonElement> SendMessageRestAsync(HttpClient client, int conversationId, string content)
    {
        var res = await client.PostAsJsonAsync($"/api/chat/conversations/{conversationId}/messages", new
        {
            content
        }, JsonOptions.CamelCase);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static async Task<HubConnection> ConnectChatHubAsync(CustomWebApplicationFactory factory, string token)
    {
        var hubUrl = new Uri(factory.Server.BaseAddress, $"hubs/chat?access_token={Uri.EscapeDataString(token)}");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .WithAutomaticReconnect()
            .Build();
        await connection.StartAsync();
        return connection;
    }

    public static async Task<HubConnection> ConnectPresenceHubAsync(CustomWebApplicationFactory factory, string token)
    {
        var hubUrl = new Uri(factory.Server.BaseAddress, $"hubs/presence?access_token={Uri.EscapeDataString(token)}");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .WithAutomaticReconnect()
            .Build();
        await connection.StartAsync();
        return connection;
    }

    public static async Task<HubConnection> ConnectCallHubAsync(CustomWebApplicationFactory factory, string token)
    {
        var hubUrl = new Uri(factory.Server.BaseAddress, $"hubs/call?access_token={Uri.EscapeDataString(token)}");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .WithAutomaticReconnect()
            .Build();
        await connection.StartAsync();
        return connection;
    }

    public static HttpClient CreateAuthenticatedClient(CustomWebApplicationFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<int> GetUserIdFromMeAsync(HttpClient client)
    {
        var me = await client.GetAsync("/api/auth/me");
        me.EnsureSuccessStatusCode();
        var json = await me.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("userId").GetInt32();
    }

    public static async Task<int> CreateGroupConversationAsync(CustomWebApplicationFactory factory, string title, params int[] userIds)
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IChatRepository>();
        var conv = Conversation.Create(true, title);
        foreach (var uid in userIds)
            conv.AddParticipant(uid);
        var created = await repo.CreateConversationAsync(conv);
        return created.ConversationId;
    }
}
