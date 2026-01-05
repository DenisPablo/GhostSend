namespace GhostSend.Domain.Interfaces;

public interface IStorageService
{
    Task<string> GuardarAsync(Stream stream, Guid id, CancellationToken cancellationToken);

    Task<Stream> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    Task EliminarAsync(Guid id, CancellationToken cancellationToken);
}