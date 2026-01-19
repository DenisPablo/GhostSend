namespace GhostSend.Domain.Exceptions;

/// <summary>
/// Base exception for the entire application, used to track which layer triggered the error.
/// </summary>
public abstract class BaseException : Exception
{
    public abstract string Layer { get; }

    protected BaseException(string message) : base(message)
    {
    }

    protected BaseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
