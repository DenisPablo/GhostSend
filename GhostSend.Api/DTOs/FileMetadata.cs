namespace GhostSend.Api.DTOs;

public record FileMetadata(
  Guid Id,
  string FileName,
  string ContentType,
  long Size,
  int Downloads,
  DateTime UploadDate,
  DateTime CurrentDate,
  DateTime? ExpirationDate,
  int? MaxDownloads
);