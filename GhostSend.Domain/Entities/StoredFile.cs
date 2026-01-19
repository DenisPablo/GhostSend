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

    public const long MaxSize = 1024 * 1024 * 1024;

    // constructor for Entity Framework
    private StoredFile() { }

    public StoredFile(string fileName, string contentType, long size, int? maxDownloads, TimeProvider timeProvider, TimeSpan? lifeTime)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(string key, string message)
        {
            if (!errors.ContainsKey(key))
            {
                errors[key] = new List<string>();
            }
            errors[key].Add(message);
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            AddError("FileName", DomainErrors.StoredFile.FileNameRequired);
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            AddError("ContentType", DomainErrors.StoredFile.ContentTypeRequired);
        }

        if (size <= 0)
        {
            AddError("Size", DomainErrors.StoredFile.NegativeSize);
        }

        if (maxDownloads.HasValue && maxDownloads <= 0)
        {
            AddError("MaxDownloads", DomainErrors.StoredFile.NegativeMaxDownloads);
        }

        if (lifeTime.HasValue && lifeTime <= TimeSpan.Zero)
        {
            AddError("LifeTime", DomainErrors.StoredFile.NegativeLifeTime);
        }

        if (size > MaxSize)
        {
            AddError("Size", DomainErrors.StoredFile.FileTooLarge);
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors.ToDictionary(k => k.Key, v => v.Value.ToArray()));
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
