using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.QA;

public class SignalRVoteChangedTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private const string QTitle = "1234567890";
    private const string QBody = "This question body has at least thirty characters.";

    public SignalRVoteChangedTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task VoteQuestion_EmitsVoteChanged_ToOtherClient_Tc060()
    {
        var ownerToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "s60o");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var q = await _client.PostAsJsonAsync("/api/questions", new { title = QTitle, body = QBody, tags = Array.Empty<string>() }, JsonOptions.CamelCase);
        var qId = (await q.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var listenerToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "s60l");
        var hubUrl = new Uri(_factory.Server.BaseAddress, $"hubs/question?access_token={Uri.EscapeDataString(listenerToken)}");

        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .WithAutomaticReconnect()
            .Build();

        var tcs = new TaskCompletionSource<JsonElement>();
        connection.On<JsonElement>("VoteChanged", payload => tcs.TrySetResult(payload));

        await connection.StartAsync();
        await connection.InvokeAsync("JoinQuestion", qId);

        var voterToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "s60v");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", voterToken);
        var voteRes = await _client.PostAsJsonAsync($"/api/votes/question/{qId}", new { voteType = "up" }, JsonOptions.CamelCase);
        voteRes.EnsureSuccessStatusCode();

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        completed.Should().Be(tcs.Task);

        var payload = await tcs.Task;
        payload.GetProperty("targetType").GetString().Should().Be("question");
        payload.GetProperty("targetId").GetInt32().Should().Be(qId);
    }
}
