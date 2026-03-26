using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public sealed class NoOpPasswordChangeCodeService : IPasswordChangeCodeService
{
    public Task<string> GenerateAndStoreCodeAsync(int userId, CancellationToken ct = default)
        => Task.FromResult("000000");

    public Task<bool> ValidateAndDeleteAsync(int userId, string code, CancellationToken ct = default)
        => Task.FromResult(false);
}
