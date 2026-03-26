using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Chat;

[Collection("Chat")]
public class MediaTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MediaTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    private static byte[] MinimalPngBytes =>
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public async Task UploadImage_SendMediaMessage_Broadcasts_TcC014()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "med14a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "med14b");
        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory,
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta)),
            await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb)));

        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        var clientA = ChatTestHelper.CreateAuthenticatedClient(_factory, ta);
        var convId = (await ChatTestHelper.StartConversationAsync(clientA, idB)).GetProperty("conversationId").GetInt32();

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(MinimalPngBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "test.png");

        var upload = await clientA.PostAsync("/api/media/upload", form);
        upload.EnsureSuccessStatusCode();
        var upJson = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var url = upJson.GetProperty("url").GetString()!;

        await using var connA = await ChatTestHelper.ConnectChatHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectChatHubAsync(_factory, tb);
        await connA.InvokeAsync("JoinConversation", convId);
        await connB.InvokeAsync("JoinConversation", convId);

        var tcs = new TaskCompletionSource<JsonElement>();
        connB.On<JsonElement>("ReceiveMessage", p => tcs.TrySetResult(p));

        await connA.InvokeAsync("SendMediaMessage", convId, "Image", url, "test.png", (long)MinimalPngBytes.Length, (string?)null, (string?)null);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        completed.Should().Be(tcs.Task);
        (await tcs.Task).GetProperty("messageType").GetString().Should().Be("image");
    }

    [Fact]
    public async Task UploadFile_Over25Mb_ReturnsBadRequest_TcC015()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.RegisterAndGetTokenAsync(client, "med15");
        var clientAuth = ChatTestHelper.CreateAuthenticatedClient(_factory, token);

        var oversized = new byte[26 * 1024 * 1024];
        Array.Fill(oversized, (byte)1);
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(oversized);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "file", "big.jpg");

        var res = await clientAuth.PostAsync("/api/media/upload", form);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("25");
    }
}
