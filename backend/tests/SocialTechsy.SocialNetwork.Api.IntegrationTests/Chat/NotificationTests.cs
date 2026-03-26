using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Chat;

[Collection("Chat")]
public class NotificationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task UnreadMessages_ThenRead_UpdatesPreview_TcC051()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "n51a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "n51b");
        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var clientB = ChatTestHelper.CreateAuthenticatedClient(_factory, tb);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();

        for (var i = 0; i < 3; i++)
            await ChatTestHelper.SendMessageRestAsync(clientB, convId, $"msg {i}");

        var listBefore = await clientA.GetAsync("/api/chat/conversations?page=1&pageSize=20");
        listBefore.EnsureSuccessStatusCode();
        var jsonBefore = await listBefore.Content.ReadFromJsonAsync<JsonElement>();
        var row = jsonBefore.GetProperty("items").EnumerateArray().First(e => e.GetProperty("conversationId").GetInt32() == convId);
        row.GetProperty("unreadCount").GetInt32().Should().BeGreaterThan(0);

        var read = await clientA.PutAsync($"/api/chat/conversations/{convId}/read", null);
        read.EnsureSuccessStatusCode();

        var listAfter = await clientA.GetAsync("/api/chat/conversations?page=1&pageSize=20");
        listAfter.EnsureSuccessStatusCode();
        var jsonAfter = await listAfter.Content.ReadFromJsonAsync<JsonElement>();
        var rowAfter = jsonAfter.GetProperty("items").EnumerateArray().First(e => e.GetProperty("conversationId").GetInt32() == convId);
        rowAfter.GetProperty("unreadCount").GetInt32().Should().Be(0);
    }
}
