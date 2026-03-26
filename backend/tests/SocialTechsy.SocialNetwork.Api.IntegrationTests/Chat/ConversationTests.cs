using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Chat;

[Collection("Chat")]
public class ConversationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConversationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task StartConversation_TwiceSamePair_ReturnsSameConversationId_TcC001()
    {
        var client = _factory.CreateClient();
        var tokenA = await TestAuthHelper.RegisterAndGetTokenAsync(client, "c1a");
        var tokenB = await TestAuthHelper.RegisterAndGetTokenAsync(client, "c1b");

        var clientB = ChatTestHelper.CreateAuthenticatedClient(_factory, tokenB);
        var userIdB = await ChatTestHelper.GetUserIdFromMeAsync(clientB);

        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, tokenA);
        var first = await ChatTestHelper.StartConversationAsync(clientA, userIdB);
        var convId1 = first.GetProperty("conversationId").GetInt32();

        var second = await ChatTestHelper.StartConversationAsync(clientA, userIdB);
        var convId2 = second.GetProperty("conversationId").GetInt32();

        convId2.Should().Be(convId1);
    }

    [Fact]
    public async Task GroupConversation_CreatedViaRepository_AppearsInList_TcC002()
    {
        var client = _factory.CreateClient();
        var t1 = await TestAuthHelper.RegisterAndGetTokenAsync(client, "g2a");
        var t2 = await TestAuthHelper.RegisterAndGetTokenAsync(client, "g2b");
        var t3 = await TestAuthHelper.RegisterAndGetTokenAsync(client, "g2c");
        var t4 = await TestAuthHelper.RegisterAndGetTokenAsync(client, "g2d");

        var id1 = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, t1));
        var id2 = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, t2));
        var id3 = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, t3));
        var id4 = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, t4));

        var groupId = await ChatTestHelper.CreateGroupConversationAsync(_factory, "Team Dev", id1, id2, id3, id4);

        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, t1);
        var list = await clientA.GetAsync("/api/chat/conversations?page=1&pageSize=50");
        list.EnsureSuccessStatusCode();
        var json = await list.Content.ReadFromJsonAsync<JsonElement>();
        var items = json.GetProperty("items").EnumerateArray().ToList();
        var found = items.Select(e => e.GetProperty("conversationId").GetInt32()).Contains(groupId);
        found.Should().BeTrue();
        var row = items.First(e => e.GetProperty("conversationId").GetInt32() == groupId);
        row.GetProperty("isGroupChat").GetBoolean().Should().BeTrue();
        row.GetProperty("title").GetString().Should().Be("Team Dev");
    }

    [Fact]
    public async Task SendMessageToConversation_BumpsItToTopOfList_TcC003()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "c3a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "c3b");
        var tc = await TestAuthHelper.RegisterAndGetTokenAsync(client, "c3c");
        var td = await TestAuthHelper.RegisterAndGetTokenAsync(client, "c3d");

        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var idC = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tc));
        var idD = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, td));

        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var conv1 = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();
        var conv2 = (await ChatTestHelper.StartConversationAsync(clientA, idC)).GetProperty("conversationId").GetInt32();
        var conv3 = (await ChatTestHelper.StartConversationAsync(clientA, idD)).GetProperty("conversationId").GetInt32();

        await ChatTestHelper.SendMessageRestAsync(clientA, conv3, "bump third conversation to top");

        var list = await clientA.GetAsync("/api/chat/conversations?page=1&pageSize=10");
        list.EnsureSuccessStatusCode();
        var json = await list.Content.ReadFromJsonAsync<JsonElement>();
        var firstId = json.GetProperty("items").EnumerateArray().First().GetProperty("conversationId").GetInt32();
        firstId.Should().Be(conv3);
    }

    [Fact]
    public async Task DeleteConversation_RemovesForUserOnly_TcC050()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "c50a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "c50b");

        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();

        var del = await clientA.DeleteAsync($"/api/chat/conversations/{convId}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listA = await clientA.GetAsync("/api/chat/conversations");
        listA.EnsureSuccessStatusCode();
        var itemsA = (await listA.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray()
            .Select(e => e.GetProperty("conversationId").GetInt32()).ToList();
        itemsA.Should().NotContain(convId);

        var clientB = ChatTestHelper.CreateAuthenticatedClient(_factory, tb);
        var listB = await clientB.GetAsync("/api/chat/conversations");
        listB.EnsureSuccessStatusCode();
        var itemsB = (await listB.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray()
            .Select(e => e.GetProperty("conversationId").GetInt32()).ToList();
        itemsB.Should().Contain(convId);
    }
}
