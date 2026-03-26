using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Chat;

[Collection("Chat")]
public class PresenceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PresenceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task FriendConnects_AReceivesUserOnline_TcC040()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "pr40a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "pr40b");
        var idA = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta));
        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory, idA, idB);

        await using var connA = await ChatTestHelper.ConnectPresenceHubAsync(_factory, ta);
        var tcs = new TaskCompletionSource<string>();
        connA.On<string>("UserOnline", uid => tcs.TrySetResult(uid));

        await using var _ = await ChatTestHelper.ConnectPresenceHubAsync(_factory, tb);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        (await tcs.Task).Should().Be(idB.ToString());

        using var scope = _factory.Services.CreateScope();
        var mux = scope.ServiceProvider.GetService<IConnectionMultiplexer>();
        if (mux != null)
        {
            var exists = await mux.GetDatabase().KeyExistsAsync($"presence:user:{idB}");
            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task FriendDisconnects_AReceivesUserOffline_TcC041()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "pr41a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "pr41b");
        var idA = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta));
        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory, idA, idB);

        await using var connA = await ChatTestHelper.ConnectPresenceHubAsync(_factory, ta);
        var connB = await ChatTestHelper.ConnectPresenceHubAsync(_factory, tb);

        var tcs = new TaskCompletionSource<string>();
        connA.On<string>("UserOffline", uid => tcs.TrySetResult(uid));

        await connB.DisposeAsync();

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        completed.Should().Be(tcs.Task);
        (await tcs.Task).Should().Be(idB.ToString());
    }

    [Fact]
    public async Task TwoTabs_FirstCloseDoesNotOffline_SecondDoes_TcC042()
    {
        var client = _factory.CreateClient();
        var ta = await TestAuthHelper.RegisterAndGetTokenAsync(client, "pr42a");
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "pr42b");
        var idA = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, ta));
        var idB = await ChatTestHelper.GetUserIdFromMeAsync(ChatTestHelper.CreateAuthenticatedClient(_factory, tb));
        await FriendshipTestHelper.EnsureAcceptedFriendshipAsync(_factory, idA, idB);

        await using var connA = await ChatTestHelper.ConnectPresenceHubAsync(_factory, ta);
        var offlineEvents = new List<string>();
        connA.On<string>("UserOffline", uid => offlineEvents.Add(uid));

        var tab1 = await ChatTestHelper.ConnectPresenceHubAsync(_factory, tb);
        var tab2 = await ChatTestHelper.ConnectPresenceHubAsync(_factory, tb);
        await Task.Delay(300);

        await tab1.DisposeAsync();
        await Task.Delay(500);
        offlineEvents.Should().BeEmpty();

        await tab2.DisposeAsync();
        await Task.Delay(500);
        offlineEvents.Should().Contain(idB.ToString());
    }

    [Fact]
    public async Task Heartbeat_DoesNotThrow_TcC043()
    {
        var client = _factory.CreateClient();
        var tb = await TestAuthHelper.RegisterAndGetTokenAsync(client, "pr43b");
        await using var connB = await ChatTestHelper.ConnectPresenceHubAsync(_factory, tb);

        var act = async () => await connB.InvokeAsync("Heartbeat");
        await act.Should().NotThrowAsync();
    }
}
