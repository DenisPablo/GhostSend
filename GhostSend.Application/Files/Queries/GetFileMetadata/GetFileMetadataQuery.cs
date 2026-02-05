using MediatR;

namespace GhostSend.Application.Files.Queries.GetFileMetadata;

public record GetFileMetadataQuery(Guid FileId) : IRequest<GetFileMetadataDto>;

public record GetFileMetadataDto(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    int? MaxDownloads,
    DateTime UploadDate,
    int CurrentDownloads,
    DateTime? ExpirationDate
);
