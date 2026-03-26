using FluentAssertions;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Security;

[Trait("Category", "Security")]
public class SecurityRateLimitTests
{
    [Fact(Skip = "TC-SC-010: Trong Environment=Testing, Program.cs gắn GetNoLimiter cho policy auth — không assert được 429 trong integration test. Kiểm thử staging/manual hoặc refactor cấu hình.")]
    public void Tc_Sc010_RateLimitOnAuth_DocumentationOnly()
    {
        true.Should().BeFalse();
    }
}
