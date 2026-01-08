using GhostSend.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GhostSend.Infrastructure.Persistence.Repositories;

public class LocalStorageService : IStorageService
{
    private readonly string _basePath;

    public LocalStorageService(IConfiguration configuration)
    {
        _basePath = configuration["LocalStorage:BasePath"] ?? "uploads";

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveAsync(Stream stream, Guid id, CancellationToken cancellationToken)
    {
        var folderName = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var folderPath = Path.Combine(_basePath, folderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileName = id.ToString();
        var fullPath = Path.Combine(folderPath, fileName);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return Path.Combine(folderName, fileName);
    }

    public Task<Stream> GetAsync(Guid id, string storagePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.Combine(_basePath, storagePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File not found in storage.", fullPath);
        }

        return Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true));
    }

    public Task DeleteAsync(Guid id, string storagePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.Combine(_basePath, storagePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}