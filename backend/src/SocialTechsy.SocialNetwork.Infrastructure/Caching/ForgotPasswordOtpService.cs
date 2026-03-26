using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using StackExchange.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public class ForgotPasswordOtpService : IForgotPasswordOtpService
{
    private readonly IDatabase? _db;
    private readonly ILogger<ForgotPasswordOtpService> _logger;
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(10);

    public ForgotPasswordOtpService(IConnectionMultiplexer? redis, ILogger<ForgotPasswordOtpService> logger)
    {
        _logger = logger;
        if (redis == null || !redis.IsConnected)
        {
            _logger.LogWarning("Redis unavailable - forgot password OTP features disabled");
            _db = null;
            return;
        }
        try { _db = redis.GetDatabase(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Redis - forgot password OTP disabled");
            _db = null;
        }
    }

    private static string Key(string email) =>
        $"fp:otp:{ComputeEmailHash(email)}";

    private static string ComputeEmailHash(string email) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant())))[..16].ToLowerInvariant();

    public async Task<string> GenerateAndStoreOtpAsync(string email, CancellationToken ct = default)
    {
        if (_db == null)
            throw new InvalidOperationException("Forgot password OTP service is not available.");

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var hash = ComputeEmailHash(email.Trim().ToLowerInvariant());
        await _db.StringSetAsync(Key(email), code, OtpTtl);
        _logger.LogInformation("Forgot password OTP generated for email hash {Hash}", hash);
        return code;
    }

    public async Task<bool> ValidateAndDeleteAsync(string email, string code, CancellationToken ct = default)
    {
        if (_db == null)
            return false;

        var stored = await _db.StringGetDeleteAsync(Key(email));
        if (!stored.HasValue)
            return false;

        var isValid = string.Equals(stored.ToString(), code.Trim(), StringComparison.Ordinal);
        if (!isValid)
            _logger.LogWarning("Invalid forgot password OTP for email hash {Hash}",
                ComputeEmailHash(email.Trim().ToLowerInvariant()));
        return isValid;
    }
}
