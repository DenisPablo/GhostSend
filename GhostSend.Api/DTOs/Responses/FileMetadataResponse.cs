namespace GhostSend.Api.DTOs.Responses;

public record FileMetadataResponse(
    Guid FileId,
    string FileName,
    string ContentType,
    long FileSize,
    int? MaxDownloads,
    int CurrentDownloads,
    DateTime UploadDate,
    DateTime? ExpirationDate,
    string? LifeTime
);