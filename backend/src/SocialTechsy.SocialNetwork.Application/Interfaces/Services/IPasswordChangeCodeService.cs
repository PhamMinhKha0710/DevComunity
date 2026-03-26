namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface IPasswordChangeCodeService
{
    Task<string> GenerateAndStoreCodeAsync(int userId, CancellationToken ct = default);
    Task<bool> ValidateAndDeleteAsync(int userId, string code, CancellationToken ct = default);
}
