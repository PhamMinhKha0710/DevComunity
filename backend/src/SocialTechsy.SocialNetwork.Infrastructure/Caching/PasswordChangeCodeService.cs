using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using StackExchange.Redis;
using System.Security.Cryptography;

namespace SocialTechsy.SocialNetwork.Infrastructure.Caching;

public class PasswordChangeCodeService : IPasswordChangeCodeService
{
    private readonly IDatabase? _db;
    private readonly ILogger<PasswordChangeCodeService> _logger;
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(10);

    public PasswordChangeCodeService(IConnectionMultiplexer? redis, ILogger<PasswordChangeCodeService> logger)
    {
        _logger = logger;
        if (redis == null || !redis.IsConnected)
        {
            _logger.LogWarning("Redis unavailable - password change code features disabled");
            _db = null;
            return;
        }
        try { _db = redis.GetDatabase(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Redis - password change code disabled");
            _db = null;
        }
    }

    private string Key(int userId) => $"pwdchange:code:{userId}";

    public async Task<string> GenerateAndStoreCodeAsync(int userId, CancellationToken ct = default)
    {
        if (_db == null)
            throw new InvalidOperationException("Password change code service is not available.");

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        await _db.StringSetAsync(Key(userId), code, CodeTtl);
        _logger.LogInformation("Password change code generated for user {UserId}", userId);
        return code;
    }

    public async Task<bool> ValidateAndDeleteAsync(int userId, string code, CancellationToken ct = default)
    {
        if (_db == null)
            return false;

        var stored = await _db.StringGetDeleteAsync(Key(userId));
        if (!stored.HasValue)
            return false;

        var isValid = string.Equals(stored.ToString(), code.Trim(), StringComparison.Ordinal);
        if (!isValid)
            _logger.LogWarning("Invalid password change code for user {UserId}", userId);
        return isValid;
    }
}
