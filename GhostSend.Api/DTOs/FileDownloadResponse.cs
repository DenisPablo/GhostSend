namespace GhostSend.Api.DTOs;

public record FileDownloadResponse(
  Stream Stream,
  string FileName,
  string ContentType
);