using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Chat;

[Collection("Chat")]
public class MessageTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MessageTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task GetMessages_TwoPages_NoOverlap_TcC004()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m4a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m4b");
        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();

        for (var i = 0; i < 50; i++)
            await ChatTestHelper.SendMessageRestAsync(clientA, convId, $"message number {i} with padding");

        var p1 = await clientA.GetAsync($"/api/chat/conversations/{convId}/messages?page=1&pageSize=20");
        var p2 = await clientA.GetAsync($"/api/chat/conversations/{convId}/messages?page=2&pageSize=20");
        p1.EnsureSuccessStatusCode();
        p2.EnsureSuccessStatusCode();

        var j1 = await p1.Content.ReadFromJsonAsync<JsonElement>();
        var j2 = await p2.Content.ReadFromJsonAsync<JsonElement>();
        var ids1 = j1.GetProperty("items").EnumerateArray().Select(m => ChatTestHelper.JsonLong(m, "messageId")).ToHashSet();
        var ids2 = j2.GetProperty("items").EnumerateArray().Select(m => ChatTestHelper.JsonLong(m, "messageId")).ToHashSet();

        ids1.Should().NotBeEmpty();
        ids2.Should().NotBeEmpty();
        ids1.Intersect(ids2).Should().BeEmpty();
    }

    [Fact]
    public async Task ChatHub_SendMessage_BroadcastsReceiveMessage_TcC010()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m10a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m10b");
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

        var tcs = new TaskCompletionSource<JsonElement>();
        connB.On<JsonElement>("ReceiveMessage", payload => tcs.TrySetResult(payload));

        await connA.InvokeAsync("SendMessage", convId, "hello from hub", (string?)null);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        var msg = await tcs.Task;
        msg.GetProperty("content").GetString().Should().Be("hello from hub");
    }

    [Fact]
    public async Task AcknowledgeDelivery_EmitsDeliveryStatusUpdated_TcC011()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m11a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m11b");
        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory,
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta)),
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb)));

        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();

        var sent = await ChatTestHelper.SendMessageRestAsync(clientA, convId, "delivery test");
        var msgId = ChatTestHelper.JsonLong(sent, "messageId");

        await using var connA = await ChatTestHelper.ConnectChatHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectChatHubAsync(_factory, tb);
        await connA.InvokeAsync("JoinConversation", convId);
        await connB.InvokeAsync("JoinConversation", convId);

        var tcs = new TaskCompletionSource<JsonElement>();
        connA.On<JsonElement>("DeliveryStatusUpdated", p => tcs.TrySetResult(p));

        await connB.InvokeAsync("AcknowledgeDelivery", convId, msgId, "Delivered");

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        ChatTestHelper.JsonLong(await tcs.Task, "messageId").Should().Be(msgId);
    }

    [Fact]
    public async Task MarkAsReadHub_EmitsMessagesRead_TcC012()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m12a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m12b");
        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory,
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta)),
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb)));

        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();
        var sent = await ChatTestHelper.SendMessageRestAsync(clientA, convId, "read me");
        var msgId = ChatTestHelper.JsonLong(sent, "messageId");

        await using var connA = await ChatTestHelper.ConnectChatHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectChatHubAsync(_factory, tb);
        await connA.InvokeAsync("JoinConversation", convId);
        await connB.InvokeAsync("JoinConversation", convId);

        var tcs = new TaskCompletionSource<JsonElement>();
        connA.On<JsonElement>("MessagesRead", p => tcs.TrySetResult(p));

        await connB.InvokeAsync("MarkAsRead", convId, msgId);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        ChatTestHelper.JsonLong(await tcs.Task, "lastMessageId").Should().Be(msgId);
    }

    [Fact]
    public async Task OfflineRecipient_CanFetchMessagesViaRest_TcC013()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m13a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m13b");
        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();
        await ChatTestHelper.SendMessageRestAsync(clientA, convId, "offline persistence check");

        var clientB = ChatTestHelper.CreateAuthenticatedClient(_factory, tb);
        var res = await clientB.GetAsync($"/api/chat/conversations/{convId}/messages?page=1&pageSize=20");
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("items").EnumerateArray().Any(m => m.GetProperty("content").GetString() == "offline persistence check")
            .Should().BeTrue();
    }

    [Fact]
    public async Task SendMessage_WithReply_IncludesReplyTo_TcC016()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m16a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m16b");
        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory,
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta)),
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb)));

        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();
        var first = await ChatTestHelper.SendMessageRestAsync(clientA, convId, "original msg");
        var firstId = ChatTestHelper.JsonLong(first, "messageId").ToString();

        await using var connA = await ChatTestHelper.ConnectChatHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectChatHubAsync(_factory, tb);
        await connA.InvokeAsync("JoinConversation", convId);
        await connB.InvokeAsync("JoinConversation", convId);

        var tcs = new TaskCompletionSource<JsonElement>();
        connB.On<JsonElement>("ReceiveMessage", p => tcs.TrySetResult(p));

        await connA.InvokeAsync("SendMessage", convId, "reply text", firstId);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        var payload = await tcs.Task;
        var replyId = ChatTestHelper.JsonLong(payload, "replyToMessageId");
        replyId.Should().Be(long.Parse(firstId));
    }

    [Fact]
    public async Task EditMessageHub_EmitsMessageEdited_TcC017()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m17a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m17b");
        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory,
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta)),
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb)));

        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();
        var sent = await ChatTestHelper.SendMessageRestAsync(clientA, convId, "editable");
        var msgId = ChatTestHelper.JsonLong(sent, "messageId");

        await using var connA = await ChatTestHelper.ConnectChatHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectChatHubAsync(_factory, tb);
        await connA.InvokeAsync("JoinConversation", convId);
        await connB.InvokeAsync("JoinConversation", convId);

        var tcs = new TaskCompletionSource<JsonElement>();
        connB.On<JsonElement>("MessageEdited", p => tcs.TrySetResult(p));

        await connA.InvokeAsync("EditMessage", convId, msgId, "edited content");

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        (await tcs.Task).GetProperty("newContent").GetString().Should().Contain("edited");
    }

    [Fact]
    public async Task DeleteMessageHub_EmitsMessageDeleted_TcC018()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m18a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m18b");
        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory,
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta)),
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb)));

        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();
        var sent = await ChatTestHelper.SendMessageRestAsync(clientA, convId, "to delete");
        var msgId = ChatTestHelper.JsonLong(sent, "messageId");

        await using var connA = await ChatTestHelper.ConnectChatHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectChatHubAsync(_factory, tb);
        await connA.InvokeAsync("JoinConversation", convId);
        await connB.InvokeAsync("JoinConversation", convId);

        var tcs = new TaskCompletionSource<JsonElement>();
        connB.On<JsonElement>("MessageDeleted", p => tcs.TrySetResult(p));

        await connA.InvokeAsync("DeleteMessage", convId, msgId);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        ChatTestHelper.JsonLong(await tcs.Task, "messageId").Should().Be(msgId);
    }

    [Fact]
    public async Task SendMessage_SanitizesXssContent_TcC052()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m52a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "m52b");
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

        var tcs = new TaskCompletionSource<JsonElement>();
        connB.On<JsonElement>("ReceiveMessage", p => tcs.TrySetResult(p));

        var xss = "<script>alert(1)</script>";
        await connA.InvokeAsync("SendMessage", convId, xss, (string?)null);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        var content = (await tcs.Task).GetProperty("content").GetString();
        content.Should().NotContain("<script>");
        content.Should().Contain("&lt;script&gt;");
    }
}
