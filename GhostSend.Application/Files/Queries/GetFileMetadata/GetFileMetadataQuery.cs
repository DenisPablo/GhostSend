using MediatR;

namespace GhostSend.Application.Files.Queries.GetFileMetadata;

public record GetFileMetadataQuery(Guid FileId) : IRequest<GetFileMetadataDto>;

public record GetFileMetadataDto(
    Guid Id,
    string Name,
    string ContentType,
    long Size,
    int? MaxDownloads,
    int CurrentDownloads,
    DateTime ExpirationDate
);
