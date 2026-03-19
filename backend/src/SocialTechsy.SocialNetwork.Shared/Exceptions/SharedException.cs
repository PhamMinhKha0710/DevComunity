namespace SocialTechsy.SocialNetwork.Shared.Exceptions;

public abstract class SharedException : Exception
{
    protected SharedException(string message) : base(message) { }
    protected SharedException(string message, Exception inner) : base(message, inner) { }
}
