using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GhostSend.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new GhostSend.Domain.Exceptions.ConcurrencyException("A concurrency conflict occurred while saving changes.", ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}