using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

internal static class SecurityJwtHelper
{
    internal const string TestSecretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    internal const string TestIssuer = "SocialTechsy.SocialNetwork";
    internal const string TestAudience = "SocialTechsy.SocialNetworkUsers";

    internal static string CreateExpiredAccessToken(string sub = "1")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: [new Claim(ClaimTypes.NameIdentifier, sub)],
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddMinutes(-30),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    internal static string TamperSignature(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return jwt;
        var sig = parts[2];
        if (sig.Length == 0)
            return jwt + "x";
        var last = sig[^1];
        var flipped = last == 'a' ? 'b' : 'a';
        parts[2] = sig[..^1] + flipped;
        return string.Join(".", parts);
    }
}
