using MediatR;

namespace GhostSend.Application.Files.Commands.UploadFile;

public record UploadFileCommand(
    Stream Stream,
    string FileName,
    string ContentType,
    long Size,
    int? MaxDownloads,
    TimeSpan? LifeTime
) : IRequest<Guid>;