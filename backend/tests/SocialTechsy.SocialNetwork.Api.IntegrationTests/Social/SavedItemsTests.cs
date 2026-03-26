using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Social;

public class SavedItemsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SavedItemsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task SaveQuestionAnswerPost_List_Unsave_TcS050_TcS051()
    {
        var (client, _) = await SocialTestHelper.RegisterUserAsync(_factory, "sv" + Guid.NewGuid().ToString("N")[..6]);

        var q = await client.PostAsJsonAsync("/api/questions", new
        {
            title = "Title save test " + Guid.NewGuid().ToString("N")[..8],
            body = "Body must be long enough for validator thirty chars minimum here.",
            tags = Array.Empty<string>()
        }, JsonOptions.CamelCase);
        q.EnsureSuccessStatusCode();
        var qId = (await q.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        var ans = await client.PostAsJsonAsync("/api/answers", new
        {
            questionId = qId,
            body = "Answer body long enough for validation rules minimum length."
        }, JsonOptions.CamelCase);
        ans.EnsureSuccessStatusCode();
        var aId = (await ans.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("answerId").GetInt32();

        var post = await SocialTestHelper.CreatePostAsync(client, "post to save", "public");
        var postId = post.GetProperty("postId").GetInt32();

        (await client.PostAsync($"/api/saveditems/questions/{qId}", null)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsync($"/api/saveditems/answers/{aId}", null)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsync($"/api/saveditems/posts/{postId}", null)).StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await client.GetAsync("/api/saveditems?page=1&pageSize=50");
        list.EnsureSuccessStatusCode();

        (await client.DeleteAsync($"/api/saveditems/questions/{qId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SaveQuestionTwice_Idempotent201_TcS052()
    {
        var (client, _) = await SocialTestHelper.RegisterUserAsync(_factory, "sv2" + Guid.NewGuid().ToString("N")[..6]);
        var q = await client.PostAsJsonAsync("/api/questions", new
        {
            title = "Dup save " + Guid.NewGuid().ToString("N")[..8],
            body = "Body must be long enough for validator thirty chars minimum here.",
            tags = Array.Empty<string>()
        }, JsonOptions.CamelCase);
        q.EnsureSuccessStatusCode();
        var qId = (await q.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("questionId").GetInt32();

        (await client.PostAsync($"/api/saveditems/questions/{qId}", null)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsync($"/api/saveditems/questions/{qId}", null)).StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
