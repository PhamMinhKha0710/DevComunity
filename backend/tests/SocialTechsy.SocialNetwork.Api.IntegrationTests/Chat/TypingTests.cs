using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Chat;

[Collection("Chat")]
public class TypingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TypingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task TypingIndicator_StartAndStop_TcC019()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "typa");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "typb");
        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory,
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta)),
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb)));

        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();

        await using var connA = await ChatTestHelper.ConnectChatHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectChatHubAsync(_factory, tb);
        await connA.InvokeAsync("JoinConversation", convId);
        await connB.InvokeAsync("JoinConversation", convId);

        var events = new List<JsonElement>();
        connB.On<JsonElement>("UserTyping", p => events.Add(p));

        await connA.InvokeAsync("Typing", convId, true);
        await Task.Delay(800);
        await connA.InvokeAsync("Typing", convId, false);
        await Task.Delay(800);

        events.Should().NotBeEmpty();
        events.Should().Contain(e => e.GetProperty("isTyping").GetBoolean());
        events.Should().Contain(e => !e.GetProperty("isTyping").GetBoolean());
    }
}
