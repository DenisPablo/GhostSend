namespace GhostSend.Domain.Exceptions;

public abstract class DomainException : BaseException
{
    public override string Layer => "Domain";

    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
