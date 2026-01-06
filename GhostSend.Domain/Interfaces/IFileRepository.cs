using GhostSend.Domain.Entities;

namespace GhostSend.Domain.Interfaces;

public interface IFileRepository
{
    Task UploadAsync(StoredFile file, CancellationToken cancellationToken);
    Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(StoredFile file, CancellationToken cancellationToken);
    Task DeleteAsync(StoredFile file, CancellationToken cancellationToken);

    Task<List<StoredFile>> GetExpiredFilesAsync(CancellationToken cancellationToken);
}
