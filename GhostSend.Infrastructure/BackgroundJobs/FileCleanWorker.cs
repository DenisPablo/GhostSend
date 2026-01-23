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
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_cleanUpInterval, stoppingToken);
        }
    }
}