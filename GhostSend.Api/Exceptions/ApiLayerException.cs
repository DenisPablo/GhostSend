using GhostSend.Domain.Exceptions;

namespace GhostSend.Api.Exceptions;

public class ApiLayerException : BaseException
{
    public override string Layer => "Api";

    public ApiLayerException(string message) : base(message)
    {
    }

    public ApiLayerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
