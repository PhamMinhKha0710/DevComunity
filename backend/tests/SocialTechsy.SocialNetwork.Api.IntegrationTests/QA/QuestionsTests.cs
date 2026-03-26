using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.QA;

public class QuestionsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private const string ValidTitle = "1234567890";
    private const string ValidBody = "This question body has at least thirty characters.";

    public QuestionsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task CreateQuestion_Returns201_WithQuestionId_Tc001()
    {
        var token = await TestAuthHelper.RegisterAndGetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _client.PostAsJsonAsync("/api/questions", new
        {
            title = ValidTitle,
            body = ValidBody,
            tags = Array.Empty<string>()
        }, JsonOptions.CamelCase);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        res.Headers.Location.Should().NotBeNull();
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("questionId").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetQuestions_ReturnsPaginated_Tc004()
    {
        var token = await TestAuthHelper.RegisterAndGetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync("/api/questions", new { title = ValidTitle, body = ValidBody, tags = Array.Empty<string>() }, JsonOptions.CamelCase);

        _client.DefaultRequestHeaders.Authorization = null;
        var res = await _client.GetAsync("/api/questions?page=1&pageSize=10");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        json.TryGetProperty("items", out _).Should().BeTrue();
        json.TryGetProperty("totalCount", out _).Should().BeTrue();
        json.TryGetProperty("page", out var page).Should().BeTrue();
        page.GetInt32().Should().Be(1);
        json.GetProperty("pageSize").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task GetQuestionById_Returns200_Tc005()
    {
        var token = await TestAuthHelper.RegisterAndGetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var create = await _client.PostAsJsonAsync("/api/questions", new { title = ValidTitle, body = ValidBody, tags = Array.Empty<string>() }, JsonOptions.CamelCase);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var qId = created.GetProperty("questionId").GetInt32();

        var res = await _client.GetAsync($"/api/questions/{qId}");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("questionId").GetInt32().Should().Be(qId);
        json.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("body").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetQuestion_MultipleTimes_IncreasesViewCount_Tc006()
    {
        var token = await TestAuthHelper.RegisterAndGetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var create = await _client.PostAsJsonAsync("/api/questions", new { title = ValidTitle, body = ValidBody, tags = Array.Empty<string>() }, JsonOptions.CamelCase);
        var qId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var first = await _client.GetAsync($"/api/questions/{qId}");
        await Task.Delay(400);
        var second = await _client.GetAsync($"/api/questions/{qId}");
        await Task.Delay(400);

        var v1 = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("viewCount").GetInt32();
        var v2 = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("viewCount").GetInt32();

        v2.Should().BeGreaterThan(v1);
    }

    [Fact]
    public async Task GetQuestion_SameUserRepeated_IncrementsCounterEachTime_Tc007()
    {
        var token = await TestAuthHelper.RegisterAndGetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var create = await _client.PostAsJsonAsync("/api/questions", new { title = ValidTitle, body = ValidBody, tags = Array.Empty<string>() }, JsonOptions.CamelCase);
        var qId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var a = await _client.GetAsync($"/api/questions/{qId}");
        await Task.Delay(400);
        var b = await _client.GetAsync($"/api/questions/{qId}");
        await Task.Delay(400);

        var va = (await a.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("viewCount").GetInt32();
        var vb = (await b.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("viewCount").GetInt32();

        vb.Should().BeGreaterThan(va);
    }

    [Fact]
    public async Task UpdateQuestion_NonOwner_Returns403_Tc009()
    {
        var tokenA = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "owner");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var create = await _client.PostAsJsonAsync("/api/questions", new { title = ValidTitle, body = ValidBody, tags = Array.Empty<string>() }, JsonOptions.CamelCase);
        var qId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var tokenB = await TestAuthHelper.RegisterAndGetTokenAsync(_client, "other");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var put = await _client.PutAsJsonAsync($"/api/questions/{qId}", new
        {
            title = "1234567890",
            body = "Updated body text with at least thirty characters here.",
            tags = Array.Empty<string>()
        }, JsonOptions.CamelCase);

        put.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
