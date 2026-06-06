using Amazon.S3;
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

        foreach (var file in expiredFiles)
        {
            try
            {
                await storageService.DeleteAsync(file.StoragePath, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                logger.LogWarning("File not found on disk, but still queued for DB cleanup. Path: {File}", file.StoragePath);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                logger.LogWarning("File not found in S3/MinIO storage, but still queued for DB cleanup. Path: {File}", file.StoragePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error deleting file {File}, skipping DB cleanup for this file", file.StoragePath);
                continue;
            }

            try
            {
                await fileRepository.DeleteAsync(file, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Cleaned up expired file {FileId} ({Path})", file.Id, file.StoragePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove expired file {FileId} from database", file.Id);
            }
        }

        logger.LogInformation("FileCleanWorker completed");
    }
}