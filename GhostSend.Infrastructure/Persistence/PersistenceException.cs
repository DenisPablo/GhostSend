using GhostSend.Infrastructure.Common.Exceptions;

namespace GhostSend.Infrastructure.Persistence;

public class PersistenceException : InfrastructureLayerException
{
    public PersistenceException(string message) : base(message)
    {
    }

    public PersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
