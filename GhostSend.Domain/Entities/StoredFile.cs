using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Exceptions;
using ValidationException = GhostSend.Domain.Exceptions.ValidationException;

namespace GhostSend.Domain.Entities;

/// <summary>
/// Represents a file stored in the system with expiration logic based on time or download count.
/// </summary>
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
    public bool IsExpired { get; private set; } = false;

    public const long MaxSize = 10L * 1024 * 1024 * 1024;

    [Timestamp]
    public byte[]? RawVersion { get; private set; }

    // constructor for Entity Framework
    private StoredFile() { }

    /// <summary>
    /// Initializes a new instance of StoredFile with validation and calculates expiration date.
    /// </summary>
    public StoredFile(string fileName, string contentType, long size, int? maxDownloads, TimeProvider timeProvider, TimeSpan? lifeTime)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(string key, string message)
        {
            if (!errors.TryGetValue(key, out var list))
            {
                list = [];
                errors[key] = list;
            }
            list.Add(message);
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
        // 256-bit cryptographically secure random token (64 hex chars)
        DeleteToken = RandomNumberGenerator.GetHexString(64, lowercase: true);
        CurrentDownloads = 0;

        // Capture once to keep UploadDate and ExpirationDate consistent
        var now = timeProvider.GetUtcNow().UtcDateTime;
        UploadDate = now;
        FileName = fileName;
        ContentType = contentType;
        Size = size;
        MaxDownloads = maxDownloads;

        if (lifeTime.HasValue)
        {
            ExpirationDate = now.Add(lifeTime.Value);
        }

    }

    /// <summary>
    /// Sets the physical storage path after the file has been successfully saved.
    /// </summary>
    public void SetStoragePath(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ValidationException(new Dictionary<string, string[]> { { "StoragePath", [DomainErrors.StoredFile.StoragePathRequired] } });
        }

        StoragePath = storagePath;
    }

    /// <summary>
    /// Checks if the file is expired and increments download count if valid.
    /// </summary>
    /// <exception cref="ValidationException">Thrown if the file has reached its limit or time expiration.</exception>
    public void Download(DateTime now)
    {
        if (ExpirationDate.HasValue && now > ExpirationDate.Value)
        {
            IsExpired = true;
            throw new ValidationException(new Dictionary<string, string[]> { { "File", [DomainErrors.StoredFile.FileExpired] } });
        }

        if (MaxDownloads.HasValue && CurrentDownloads >= MaxDownloads.Value)
        {
            IsExpired = true;
            throw new ValidationException(new Dictionary<string, string[]> { { "File", [DomainErrors.StoredFile.FileExpired] } });
        }

        CurrentDownloads++;
        if (MaxDownloads.HasValue && CurrentDownloads >= MaxDownloads.Value)
        {
            IsExpired = true;
        }
    }

    public void MarkExpired()
    {
        IsExpired = true;
    }

    public bool IsExpiredAt(DateTime now)
    {
        if (IsExpired)
        {
            return true;
        }

        if (ExpirationDate.HasValue && now > ExpirationDate.Value)
        {
            return true;
        }

        if (MaxDownloads.HasValue && CurrentDownloads >= MaxDownloads.Value)
        {
            return true;
        }

        return false;
    }
}
