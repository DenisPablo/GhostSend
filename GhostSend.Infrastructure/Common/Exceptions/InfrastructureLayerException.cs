using GhostSend.Domain.Exceptions;

namespace GhostSend.Infrastructure.Common.Exceptions;

/// <summary>
/// Exception representing a failure within the Infrastructure layer (e.g., database or external services).
/// </summary>
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
