using GhostSend.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostSend.Infrastructure.BackgroundJobs;

public class StorageReconciliationWorker(IServiceScopeFactory serviceScopeFactory, ILogger<StorageReconciliationWorker> logger) : BackgroundService
{
    private readonly TimeSpan _reconciliationInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StorageReconciliationWorker started");

        // Delay first run to give the app time to initialize
        await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing StorageReconciliationWorker");
            }

            await Task.Delay(_reconciliationInterval, stoppingToken);
        }
    }

    public async Task ReconcileAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("StorageReconciliationWorker is running");

        using var scope = serviceScopeFactory.CreateScope();
        var fileRepository = scope.ServiceProvider.GetRequiredService<IFileRepository>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        var storageFiles = await storageService.ListFilesAsync(cancellationToken);
        var dbStoragePaths = await fileRepository.GetAllStoragePathsAsync(cancellationToken);
        var dbPathsSet = new HashSet<string>(dbStoragePaths, StringComparer.Ordinal);

        var orphanedCount = 0;
        foreach (var key in storageFiles)
        {
            if (!dbPathsSet.Contains(key))
            {
                try
                {
                    await storageService.DeleteAsync(key, cancellationToken);
                    logger.LogWarning("Deleted orphaned file from storage: {Key}", key);
                    orphanedCount++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete orphaned file {Key} from storage", key);
                }
            }
        }

        if (orphanedCount > 0)
        {
            logger.LogInformation("StorageReconciliationWorker deleted {Count} orphaned file(s)", orphanedCount);
        }
        else
        {
            logger.LogInformation("StorageReconciliationWorker completed — no orphaned files found");
        }
    }
}