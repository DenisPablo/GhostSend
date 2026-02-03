using GhostSend.Domain.Exceptions;
using GhostSend.Domain.Interfaces;
using MediatR;

namespace GhostSend.Application.Files.Queries.GetFileMetadata;

public class GetFileMetadataQueryHandler(IFileRepository fileRepository) : IRequestHandler<GetFileMetadataQuery, GetFileMetadataDto>
{
    public async Task<GetFileMetadataDto> Handle(GetFileMetadataQuery request, CancellationToken cancellationToken)
    {
        var file = await fileRepository.GetByIdAsync(request.FileId, cancellationToken) ??
            throw new NotFoundException("File", request.FileId);

        return new GetFileMetadataDto(
            file.Id,
            file.FileName,
            file.ContentType,
            file.Size,
            file.MaxDownloads,
            file.CurrentDownloads,
            file.ExpirationDate ?? DateTime.MaxValue
        );
    }
}
