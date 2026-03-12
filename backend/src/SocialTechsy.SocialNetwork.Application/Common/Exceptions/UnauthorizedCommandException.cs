namespace SocialTechsy.SocialNetwork.Application.Common.Exceptions;

/// <summary>
/// Thrown when the current user is not authorized to perform the command (maps to HTTP 403).
/// </summary>
public class UnauthorizedCommandException : Exception
{
    public UnauthorizedCommandException(string message) : base(message)
    {
    }
}
