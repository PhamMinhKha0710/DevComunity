using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Security;

[Trait("Category", "Security")]
public class SecuritySensitiveDataTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SecuritySensitiveDataTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task Tc_Sc017_Me_DoesNotExposeSecrets()
    {
        var (client, userId) = await SocialTestHelper.RegisterUserAsync(_factory, "pii" + Guid.NewGuid().ToString("N")[..6]);

        var me = await client.GetAsync("/api/auth/me");
        me.EnsureSuccessStatusCode();
        var json = await me.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("userId").GetInt32().Should().Be(userId);
        foreach (var prop in json.EnumerateObject())
        {
            prop.Name.ToLowerInvariant().Should().NotBe("passwordhash");
            prop.Name.ToLowerInvariant().Should().NotBe("password");
            prop.Name.ToLowerInvariant().Should().NotBe("refreshtoken");
        }
    }
}
