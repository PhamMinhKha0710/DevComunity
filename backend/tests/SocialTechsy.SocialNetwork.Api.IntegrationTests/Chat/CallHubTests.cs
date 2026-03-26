using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Chat;

[Collection("Chat")]
public class CallHubTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CallHubTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task InitiateCall_BReceivesIncomingCall_TcC030()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "call30a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "call30b");
        var idA = (await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta))).ToString();
        var idB = (await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb))).ToString();

        await using var connA = await ChatTestHelper.ConnectCallHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectCallHubAsync(_factory, tb);

        var tcs = new TaskCompletionSource<JsonElement>();
        connB.On<JsonElement>("IncomingCall", p => tcs.TrySetResult(p));

        await connA.InvokeAsync("InitiateCall", idB, "video", "caller");

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        var j = await tcs.Task;
        j.GetProperty("callType").GetString().Should().Be("video");
        j.GetProperty("callerId").GetString().Should().Be(idA);
    }

    [Fact]
    public async Task AcceptCall_SdpExchange_TcC031()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "call31a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "call31b");
        var idA = (await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta))).ToString();
        var idB = (await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb))).ToString();

        await using var connA = await ChatTestHelper.ConnectCallHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectCallHubAsync(_factory, tb);

        var tcsAccepted = new TaskCompletionSource<JsonElement>();
        connA.On<JsonElement>("CallAccepted", p => tcsAccepted.TrySetResult(p));

        await connB.InvokeAsync("AcceptCall", idA);

        var c1 = await Task.WhenAny(tcsAccepted.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        c1.Should().Be(tcsAccepted.Task);

        var tcsOffer = new TaskCompletionSource<JsonElement>();
        connB.On<JsonElement>("ReceiveOffer", p => tcsOffer.TrySetResult(p));
        await connA.InvokeAsync("SendOffer", idB, "fake-sdp-offer");

        var c2 = await Task.WhenAny(tcsOffer.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        c2.Should().Be(tcsOffer.Task);
        (await tcsOffer.Task).GetProperty("sdp").GetString().Should().Be("fake-sdp-offer");

        var tcsAnswer = new TaskCompletionSource<JsonElement>();
        connA.On<JsonElement>("ReceiveAnswer", p => tcsAnswer.TrySetResult(p));
        await connB.InvokeAsync("SendAnswer", idA, "fake-sdp-answer");

        var c3 = await Task.WhenAny(tcsAnswer.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        c3.Should().Be(tcsAnswer.Task);

        var tcsIce = new TaskCompletionSource<JsonElement>();
        connB.On<JsonElement>("ReceiveIceCandidate", p => tcsIce.TrySetResult(p));
        await connA.InvokeAsync("SendIceCandidate", idB, "ice-1");

        var c4 = await Task.WhenAny(tcsIce.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        c4.Should().Be(tcsIce.Task);
    }

    [Fact]
    public async Task RejectCall_AReceives_TcC032()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "call32a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "call32b");
        var idA = (await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta))).ToString();

        await using var connA = await ChatTestHelper.ConnectCallHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectCallHubAsync(_factory, tb);

        var tcs = new TaskCompletionSource<JsonElement>();
        connA.On<JsonElement>("CallRejected", p => tcs.TrySetResult(p));

        await connB.InvokeAsync("RejectCall", idA, "busy");

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        (await tcs.Task).GetProperty("reason").GetString().Should().Be("busy");
    }

    [Fact]
    public async Task EndCall_PeerReceives_TcC033()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "call33a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "call33b");
        var idA = (await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta))).ToString();
        var idB = (await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb))).ToString();

        await using var connA = await ChatTestHelper.ConnectCallHubAsync(_factory, ta);
        await using var connB = await ChatTestHelper.ConnectCallHubAsync(_factory, tb);

        var tcs = new TaskCompletionSource<JsonElement>();
        connB.On<JsonElement>("CallEnded", p => tcs.TrySetResult(p));

        await connA.InvokeAsync("EndCall", idB);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        (await tcs.Task).GetProperty("userId").GetString().Should().Be(idA);
    }

    [Fact]
    public async Task DisconnectCallHub_ConnectionClosed_TcC034()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "call34a");
        var connA = await ChatTestHelper.ConnectCallHubAsync(_factory, ta);
        await connA.DisposeAsync();
        connA.State.Should().Be(HubConnectionState.Disconnected);
    }
}
