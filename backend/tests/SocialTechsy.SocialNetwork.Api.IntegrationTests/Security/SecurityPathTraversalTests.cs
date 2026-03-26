using System.Net;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Security;

[Trait("Category", "Security")]
public class SecurityPathTraversalTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityPathTraversalTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureDatabaseCreated();
    }

    [Theory]
    [InlineData("..%2F..%2F..%2Fetc%2Fpasswd")]
    [InlineData("../../../etc/passwd")]
    [InlineData("foo")]
    public async Task Tc_Sc019_Media_InvalidOrTraversalId_Returns404(string id)
    {
        var response = await _client.GetAsync($"/api/media/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
