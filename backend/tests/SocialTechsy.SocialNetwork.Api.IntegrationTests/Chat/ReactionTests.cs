using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Chat;

[Collection("Chat")]
public class ReactionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReactionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task PostReaction_AndHubBroadcastsReceiveReaction_TcC020()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "r20a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "r20b");
        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory,
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta)),
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb)));

        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var clientB = ChatTestHelper.CreateAuthenticatedClient(_factory, tb);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();
        var sent = await ChatTestHelper.SendMessageRestAsync(clientA, convId, "react to me");
        var msgId = ChatTestHelper.JsonLong(sent, "messageId");

        await using var connA = await ChatTestHelper.ConnectChatHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectChatHubAsync(_factory, tb);
        await connA.InvokeAsync("JoinConversation", convId);
        await connB.InvokeAsync("JoinConversation", convId);

        var tcs = new TaskCompletionSource<JsonElement>();
        connA.On<JsonElement>("ReceiveReaction", p => tcs.TrySetResult(p));

        await connB.InvokeAsync("AddReaction", convId, msgId.ToString(), "Like");

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        ChatTestHelper.JsonLong(await tcs.Task, "messageId").Should().Be(msgId);
    }

    [Fact]
    public async Task ReactionTypes_ReplaceAndRemove_TcC021()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "r21a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "r21b");
        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var clientB = ChatTestHelper.CreateAuthenticatedClient(_factory, tb);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();
        var sent = await ChatTestHelper.SendMessageRestAsync(clientA, convId, "reactions");
        var msgId = ChatTestHelper.JsonLong(sent, "messageId");

        foreach (var t in new[] { "Like", "Love", "Haha", "Wow", "Sad", "Angry" })
        {
            var r = await clientB.PostAsJsonAsync($"/api/chat/messages/{msgId}/reactions", new { reactionType = t },
                JsonOptions.CamelCase);
            r.EnsureSuccessStatusCode();
            var dto = await r.Content.ReadFromJsonAsync<JsonElement>();
            dto.GetProperty("reactionType").GetString().Should().Be(t.ToString().ToLowerInvariant());
        }

        var del = await clientB.DeleteAsync($"/api/chat/messages/{msgId}/reactions");
        del.EnsureSuccessStatusCode();
    }
}
