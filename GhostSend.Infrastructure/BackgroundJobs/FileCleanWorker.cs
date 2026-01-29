using System.Collections.Concurrent;
using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostSend.Infrastructure.BackgroundJobs;

public class FileCleanWorker(IServiceScopeFactory serviceScopeFactory, ILogger<FileCleanWorker> logger) : BackgroundService
{
    private readonly TimeSpan _cleanUpInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FileCleanWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredFilesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing FileCleanWorker");
            }

            await Task.Delay(_cleanUpInterval, stoppingToken);
        }
    }

    public async Task CleanupExpiredFilesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("FileCleanWorker is running");

        using var scope = serviceScopeFactory.CreateScope();
        var fileRepository = scope.ServiceProvider.GetRequiredService<IFileRepository>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var expiredFiles = await fileRepository.GetExpiredFilesAsync(cancellationToken);
        var filesToDeleteCorrectly = new ConcurrentBag<StoredFile>();

        var task = expiredFiles.Select(async file =>
        {
            try
            {
                await storageService.DeleteAsync(file.StoragePath, cancellationToken);
                filesToDeleteCorrectly.Add(file);
            }
            catch
            {
                logger.LogError("Error deleting file {File}", file.StoragePath);
            }
        });

        await Task.WhenAll(task);

        foreach (var file in filesToDeleteCorrectly)
        {
            await fileRepository.DeleteAsync(file, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("FileCleanWorker completed");
    }
}