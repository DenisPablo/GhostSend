namespace GhostSend.Domain.Interfaces;

public interface IStorageService
{
    Task<string> SaveAsync(Stream stream, Guid id, CancellationToken cancellationToken);

    Task<Stream> GetAsync(Guid id, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}