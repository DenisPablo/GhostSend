using MediatR;

namespace GhostSend.Application.Files.Queries.DownloadFile;

public record DownloadFileQuery(Guid FileId) : IRequest<DownloadFileQueryResult>;

public record DownloadFileQueryResult(Stream Stream, string FileName, string ContentType, long Size);
