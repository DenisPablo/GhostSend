using GhostSend.Domain.Exceptions;

namespace GhostSend.Infrastructure.Common.Exceptions;

public class InfrastructureLayerException : BaseException
{
    public override string Layer => "Infrastructure";

    public InfrastructureLayerException(string message) : base(message)
    {
    }

    public InfrastructureLayerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
