namespace GhostSend.Domain.Interfaces;

public interface IStorageService
{
    Task<string> SaveAsync(Stream stream, Guid id, CancellationToken cancellationToken);

    Task<Stream> GetAsync(Guid id, string storagePath, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, string storagePath, CancellationToken cancellationToken);
}