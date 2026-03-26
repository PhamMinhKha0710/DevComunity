using System.Text.Json;
using System.Text.Json.Serialization;

namespace SocialTechsy.SocialNetwork.Api.IntegrationTests.Infrastructure;

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
