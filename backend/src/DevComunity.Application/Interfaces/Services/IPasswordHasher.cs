namespace DevComunity.Application.Interfaces.Services;

/// <summary>
/// Service interface for password hashing
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}
