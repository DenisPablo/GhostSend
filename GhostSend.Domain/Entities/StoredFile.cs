using System;

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

    public StoredFile(string fileName, string contentType, long size, DateTime uploadDate, int? maxDownloads, TimeSpan? lifeTime)
    {
        Id = Guid.NewGuid();
        DeleteToken = Guid.NewGuid().ToString("N");

        FileName = fileName;
        ContentType = contentType;
        Size = size;
        UploadDate = uploadDate;

        CurrentDownloads = 0;

        MaxDownloads = maxDownloads;

        if (lifeTime.HasValue)
        {
            ExpirationDate = DateTime.UtcNow.Add(lifeTime.Value);
        }
    }

    public void SetStoragePath(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("The storage path cannot be null or empty.");
        }

        StoragePath = storagePath;
    }

    public void IncrementDownloads()
    {
        CurrentDownloads++;
    }

    public bool IsExpired()
    {
        var expirationTimeReached = ExpirationDate.HasValue && DateTime.UtcNow > ExpirationDate.Value;
        var downloadsExhausted = MaxDownloads.HasValue && CurrentDownloads >= MaxDownloads.Value;

        return expirationTimeReached || downloadsExhausted;
    }
}
