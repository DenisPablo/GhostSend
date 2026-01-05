using GhostSend.Domain.Entities;

namespace GhostSend.Domain.Interfaces;

public interface IFileRepository
{
    Task SubirAsync(StoredFile file, CancellationToken cancellationToken);
    Task<StoredFile?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task ActualizarAsync(StoredFile file, CancellationToken cancellationToken);
    Task EliminarAsync(Guid id, CancellationToken cancellationToken);

    Task<List<StoredFile>> MostrarArchivosExpiradosAsync(CancellationToken cancellationToken);
}
