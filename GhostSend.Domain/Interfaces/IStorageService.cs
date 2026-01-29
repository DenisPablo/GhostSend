namespace GhostSend.Domain.Interfaces;

public interface IStorageService
{
    Task<string> SaveAsync(Stream stream, CancellationToken cancellationToken);

    Task<Stream> GetAsync(string storagePath, CancellationToken cancellationToken);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
}