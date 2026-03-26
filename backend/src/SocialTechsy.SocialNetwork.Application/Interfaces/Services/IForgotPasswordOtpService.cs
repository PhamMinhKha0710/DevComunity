namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface IForgotPasswordOtpService
{
    Task<string> GenerateAndStoreOtpAsync(string email, CancellationToken ct = default);
    Task<bool> ValidateAndDeleteAsync(string email, string code, CancellationToken ct = default);
}
