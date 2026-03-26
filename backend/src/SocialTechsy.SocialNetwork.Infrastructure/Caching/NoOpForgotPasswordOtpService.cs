using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public sealed class NoOpForgotPasswordOtpService : IForgotPasswordOtpService
{
    public Task<string> GenerateAndStoreOtpAsync(string email, CancellationToken ct = default)
        => throw new InvalidOperationException("Forgot password OTP service is not available.");

    public Task<bool> ValidateAndDeleteAsync(string email, string code, CancellationToken ct = default)
        => Task.FromResult(false);
}
