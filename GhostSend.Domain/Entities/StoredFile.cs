using System.Collections.Generic;
using System.Linq;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Exceptions;

namespace GhostSend.Domain.Entities;

public class StoredFile
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;

    public long Size { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public string DeleteToken { get; private set; } = string.Empty;

    public DateTime UploadDate { get; private set; }
    public DateTime? ExpirationDate { get; private set; }
    public int? MaxDownloads { get; private set; }

    public int CurrentDownloads { get; private set; }

    // constructor for Entity Framework
    private StoredFile() { }

    public StoredFile(string fileName, string contentType, long size, int? maxDownloads, TimeProvider timeProvider, TimeSpan? lifeTime)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors.Add(DomainErrors.StoredFile.FileNameRequired);
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            errors.Add(DomainErrors.StoredFile.ContentTypeRequired);
        }

        if (size <= 0)
        {
            errors.Add(DomainErrors.StoredFile.NegativeSize);
        }

        if (maxDownloads.HasValue && maxDownloads <= 0)
        {
            errors.Add(DomainErrors.StoredFile.NegativeMaxDownloads);
        }

        if (lifeTime.HasValue && lifeTime <= TimeSpan.Zero)
        {
            errors.Add(DomainErrors.StoredFile.NegativeLifeTime);
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "DomainValidation", errors.ToArray() }
            });
        }

        Id = Guid.NewGuid();
        DeleteToken = Guid.NewGuid().ToString("N");
        CurrentDownloads = 0;
        UploadDate = timeProvider.GetUtcNow().UtcDateTime;
        FileName = fileName;
        ContentType = contentType;
        Size = size;
        MaxDownloads = maxDownloads;

        if (lifeTime.HasValue)
        {
            ExpirationDate = timeProvider.GetUtcNow().UtcDateTime.Add(lifeTime.Value);
        }
    }

    public void SetStoragePath(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ValidationException(new Dictionary<string, string[]> { { "StoragePath", [DomainErrors.StoredFile.StoragePathRequired] } });
        }

        StoragePath = storagePath;
    }

    public void IncrementDownloads()
    {
        CurrentDownloads++;
    }

    public bool IsExpired(DateTime now)
    {
        var expirationTimeReached = ExpirationDate.HasValue && now > ExpirationDate.Value;
        var downloadsExhausted = MaxDownloads.HasValue && CurrentDownloads >= MaxDownloads.Value;

        return expirationTimeReached || downloadsExhausted;
    }
}
