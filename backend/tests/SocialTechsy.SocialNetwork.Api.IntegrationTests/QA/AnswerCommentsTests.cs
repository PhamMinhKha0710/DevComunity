using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.QA;

[Collection("Comments")]
public class AnswerCommentsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnswerCommentsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task AddCommentToAnswer_Returns201()
    {
        var token = await TestAuthHelper.RegisterAndGetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var q = await _client.PostAsJsonAsync("/api/questions",
            new { title = "Comment test Q", body = "This question body has at least thirty characters long.", tags = Array.Empty<string>() },
            JsonOptions.CamelCase);
        var qId = (await q.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var ans = await _client.PostAsJsonAsync("/api/answers",
            new { questionId = qId, body = "This answer body has at least thirty characters long." },
            JsonOptions.CamelCase);
        var ansId = (await ans.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("answerId").GetInt32();

        var comment = await _client.PostAsJsonAsync($"/api/comments/answer/{ansId}",
            new { body = "This is a test comment on an answer." },
            JsonOptions.CamelCase);

        comment.StatusCode.Should().Be(HttpStatusCode.Created, $"Got: {comment.StatusCode} - {await comment.Content.ReadAsStringAsync()}");
        var json = await comment.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("commentId").GetInt32().Should().BeGreaterThan(0);
        json.GetProperty("body").GetString().Should().Be("This is a test comment on an answer.");
    }

    [Fact]
    public async Task AddLongCommentToAnswer_Returns201()
    {
        var token = await TestAuthHelper.RegisterAndGetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var q = await _client.PostAsJsonAsync("/api/questions",
            new { title = "Long comment test Q", body = "This question body has at least thirty characters long.", tags = Array.Empty<string>() },
            JsonOptions.CamelCase);
        var qId = (await q.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var ans = await _client.PostAsJsonAsync("/api/answers",
            new { questionId = qId, body = "This answer body has at least thirty characters long." },
            JsonOptions.CamelCase);
        var ansId = (await ans.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("answerId").GetInt32();

        var longBody = new string('x', 1200);

        var comment = await _client.PostAsJsonAsync($"/api/comments/answer/{ansId}",
            new { body = longBody },
            JsonOptions.CamelCase);

        comment.StatusCode.Should().Be(HttpStatusCode.Created, $"Got: {comment.StatusCode} - {await comment.Content.ReadAsStringAsync()}");
        var json = await comment.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("commentId").GetInt32().Should().BeGreaterThan(0);
        json.GetProperty("body").GetString().Should().Be(longBody);
    }
}
