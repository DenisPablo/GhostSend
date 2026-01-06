using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GhostSend.Infrastructure.Persistence.Repositories;

public class LocalStorageService : ILocalStorageService
{
    private readonly string _basePath;

    public LocalStorageService(IConfiguration _configurations)
    {
        _basePath = _configurations["LocalStorage:BasePath"] ?? "uploads";

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var relativePath = $"{now:yyyy-MM-dd_HH-mm-ss}_{fileName}";

        var filePath = Path.Combine(_basePath, relativePath);

        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }

        var fileId = Guid.NewGuid();
        var physicalName = $"{fileId}{Path.GetExtension(fileName)}";
        var fullPath = Path.Combine(filePath, physicalName);
        var dbPath = Path.Combine(relativePath, physicalName);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return dbPath;
    }
}