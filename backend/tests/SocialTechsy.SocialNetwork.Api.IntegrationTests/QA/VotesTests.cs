using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Enums;
using SocialTechsy.SocialNetwork.Infrastructure.Persistence.Data;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.QA;

public class VotesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private const string QTitle = "1234567890";
    private const string QBody = "This question body has at least thirty characters.";

    public VotesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task VoteQuestion_Up_Returns200_WithScore_Tc040()
    {
        var ownerToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "v40o");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var q = await _client.PostAsJsonAsync("/api/questions", new { title = QTitle, body = QBody, tags = Array.Empty<string>() }, JsonOptions.CamelCase);
        var qId = (await q.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var voterToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "v40v");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", voterToken);

        var res = await _client.PostAsJsonAsync($"/api/votes/question/{qId}", new { voteType = "up" }, JsonOptions.CamelCase);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("score").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task VoteQuestion_UpTwice_ScoreUnchanged_Tc042()
    {
        var ownerToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "v42o");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var q = await _client.PostAsJsonAsync("/api/questions", new { title = QTitle, body = QBody, tags = Array.Empty<string>() }, JsonOptions.CamelCase);
        var qId = (await q.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var voterToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "v42v");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", voterToken);

        var first = await _client.PostAsJsonAsync($"/api/votes/question/{qId}", new { voteType = "up" }, JsonOptions.CamelCase);
        var s1 = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("score").GetInt32();
        var second = await _client.PostAsJsonAsync($"/api/votes/question/{qId}", new { voteType = "up" }, JsonOptions.CamelCase);
        var s2 = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("score").GetInt32();

        s2.Should().Be(s1);
    }

    [Fact]
    public async Task VotePost_Up_Returns200_WithScore_Tc045()
    {
        var token = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "v45");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await _client.GetAsync("/api/auth/me");
        me.EnsureSuccessStatusCode();
        var userId = (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetInt32();

        var postId = await AddPostAsync(_factory, userId);

        var res = await _client.PostAsJsonAsync($"/api/votes/post/{postId}", new { voteType = "up" }, JsonOptions.CamelCase);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("score").GetInt32().Should().BeGreaterThan(0);
    }

    private static async Task<int> AddPostAsync(CustomWebApplicationFactory factory, int authorId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SocialTechsySocialNetworkDbContext>();
        var post = new Post
        {
            AuthorId = authorId,
            Content = "Post content for integration test vote at least thirty chars.",
            CreatedAt = DateTime.UtcNow,
            Visibility = PostVisibility.Public
        };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        return post.PostId;
    }
}
