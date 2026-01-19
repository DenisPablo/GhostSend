using GhostSend.Domain.Exceptions;

namespace GhostSend.Application.Common.Exceptions;

public class ApplicationLayerException : BaseException
{
    public override string Layer => "Application";

    public ApplicationLayerException(string message) : base(message)
    {
    }

    public ApplicationLayerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
