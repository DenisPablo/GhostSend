namespace GhostSend.Api.DTOs;

public record FileMetadataResponse(
  Guid Id,
  string FileName,
  string ContentType,
  int CurrentDownloads,
  DateTime UploadDate,
  DateTime? ExpirationDate,
  int? MaxDownloads
);