using MediatR;

namespace GhostSend.Application.Files.Queries.GetFile;

public record DownloadFileQuery(Guid FileId) : IRequest<FileDownloadResponse>;

public record FileDownloadResponse(Stream Stream, string FileName, string ContentType, long Size);

