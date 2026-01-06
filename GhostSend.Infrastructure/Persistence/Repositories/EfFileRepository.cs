using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using GhostSend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GhostSend.Infrastructure.Persistence.Repositories;

public class EfFileRepository(ApplicationDbContext context) : IFileRepository
{

    public async Task UploadAsync(StoredFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            await context.StoredFiles.AddAsync(file, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new PersistenceException("An error occurred while preparing the file for upload.", ex);
        }
    }

    public async Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await context.StoredFiles.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new PersistenceException($"An error occurred while retrieving the file with ID: {id}.", ex);
        }
    }

    public async Task UpdateAsync(StoredFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            context.StoredFiles.Update(file);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new PersistenceException("An error occurred while updating the file metadata.", ex);
        }
    }

    public async Task DeleteAsync(StoredFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            context.StoredFiles.Remove(file);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new PersistenceException("An error occurred while marking the file for deletion.", ex);
        }
    }

    public async Task<List<StoredFile>> GetExpiredFilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await context.StoredFiles
                .Where(f => f.ExpirationDate != null && f.ExpirationDate < DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new PersistenceException("An error occurred while retrieving expired files.", ex);
        }
    }
}