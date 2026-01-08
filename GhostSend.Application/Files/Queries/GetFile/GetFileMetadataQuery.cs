namespace GhostSend.Application.Files.Queries.GetFile;

public record GetFileMetaDataDto(
    Guid Id,
    string Name,
    string ContentType,
    long Size,
    int? MaxDownloads,
    int CurrentDownloads,
    DateTime ExpirationDate
);