using MediatR;

namespace GhostSend.Application.Files.Commands.UploadFile;

public record UploadFileResponse
{
    public required Guid FileId { get; set; }
    public required string DeleteToken { get; set; }
}

public record UploadFileCommand(
    Stream Stream,
    string FileName,
    string ContentType,
    long Size,
    int? MaxDownloads,
    TimeSpan? LifeTime
) : IRequest<UploadFileResponse>;