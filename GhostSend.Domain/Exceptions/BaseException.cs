namespace GhostSend.Domain.Exceptions;

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
