using GhostSend.Domain.Entities;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Interfaces;
using GhostSend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GhostSend.Infrastructure.Persistence.Repositories;

public class EfFileRepository(ApplicationDbContext context) : IFileRepository
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
            throw new PersistenceException(DomainErrors.Persistence.FileUploadError, ex);
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
            throw new PersistenceException($"{DomainErrors.Persistence.FileRetrieveError} ID: {id}", ex);
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
            throw new PersistenceException(DomainErrors.Persistence.FileUpdateError, ex);
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
            throw new PersistenceException(DomainErrors.Persistence.FileDeleteError, ex);
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
            throw new PersistenceException(DomainErrors.Persistence.ExpiredFilesRetrieveError, ex);
        }
    }
}