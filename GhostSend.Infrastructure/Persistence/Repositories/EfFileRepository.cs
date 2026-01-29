using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using GhostSend.Infrastructure.Common.Errors;
using GhostSend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GhostSend.Infrastructure.Persistence.Repositories;

public class EfFileRepository(ApplicationDbContext context, TimeProvider timeProvider) : IFileRepository
{

    public async Task AddAsync(StoredFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            await context.StoredFiles.AddAsync(file, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new PersistenceException(InfrastructureErrors.Persistence.FileUploadError, ex);
        }
    }

    public async Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var StoredFile = await context.StoredFiles.Where(f => f.Id == id)
                                                       .Where(f => (!f.IsExpired) && (f.ExpirationDate > now || f.ExpirationDate == null))
                                                       .FirstOrDefaultAsync(cancellationToken);

            return StoredFile;
        }
        catch (Exception ex)
        {
            throw new PersistenceException($"{InfrastructureErrors.Persistence.FileRetrieveError} ID: {id}", ex);
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
            throw new PersistenceException(InfrastructureErrors.Persistence.FileUpdateError, ex);
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
            throw new PersistenceException(InfrastructureErrors.Persistence.FileDeleteError, ex);
        }
    }

    public async Task<List<StoredFile>> GetExpiredFilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var StoredFiles = await context.StoredFiles
                .Where(f => f.IsExpired == true && (f.ExpirationDate < now || f.ExpirationDate == null))
                .ToListAsync(cancellationToken);
            return StoredFiles;
        }
        catch (Exception ex)
        {
            throw new PersistenceException(InfrastructureErrors.Persistence.ExpiredFilesRetrieveError, ex);
        }
    }
}