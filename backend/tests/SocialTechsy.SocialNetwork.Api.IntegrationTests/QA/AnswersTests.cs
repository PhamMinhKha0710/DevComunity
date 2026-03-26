using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.QA;

public class AnswersTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    private const string QTitle = "1234567890";
    private const string QBody = "This question body has at least thirty characters.";
    private const string AnsBody = "This answer body has at least thirty characters long.";

    public AnswersTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task CreateAnswer_Returns201_Tc020()
    {
        var token = await TestAuthHelper.RegisterAndGetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var q = await _client.PostAsJsonAsync("/api/questions", new { title = QTitle, body = QBody, tags = Array.Empty<string>() }, JsonOptions.CamelCase);
        var qId = (await q.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var res = await _client.PostAsJsonAsync("/api/answers", new
        {
            questionId = qId,
            body = AnsBody
        }, JsonOptions.CamelCase);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("answerId").GetInt32().Should().BeGreaterThan(0);
        json.GetProperty("questionId").GetInt32().Should().Be(qId);
    }

    [Fact]
    public async Task AcceptAnswer_ByOwner_Returns200_Tc021()
    {
        var ownerToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "own");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var q = await _client.PostAsJsonAsync("/api/questions", new { title = QTitle, body = QBody, tags = Array.Empty<string>() }, JsonOptions.CamelCase);
        var qId = (await q.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var answererToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "ans");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", answererToken);
        var ans = await _client.PostAsJsonAsync("/api/answers", new { questionId = qId, body = AnsBody }, JsonOptions.CamelCase);
        var answerId = (await ans.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("answerId").GetInt32();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var accept = await _client.PostAsync($"/api/answers/{answerId}/accept?questionId={qId}", null);

        accept.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AcceptAnswer_ByNonOwner_Returns400_Tc022()
    {
        var ownerToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "o2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var q = await _client.PostAsJsonAsync("/api/questions", new { title = QTitle, body = QBody, tags = Array.Empty<string>() }, JsonOptions.CamelCase);
        var qId = (await q.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var answererToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "a2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", answererToken);
        var ans = await _client.PostAsJsonAsync("/api/answers", new { questionId = qId, body = AnsBody }, JsonOptions.CamelCase);
        var answerId = (await ans.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("answerId").GetInt32();

        var intruderToken = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "intr");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", intruderToken);
        var accept = await _client.PostAsync($"/api/answers/{answerId}/accept?questionId={qId}", null);

        accept.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var text = await accept.Content.ReadAsStringAsync();
        text.Should().Contain("authorized");
    }
}
