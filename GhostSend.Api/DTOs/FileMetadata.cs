namespace GhostSend.Api.DTOs;

public record FileMetadata(
  Guid Id,
  string FileName,
  string ContentType,
  int Downloads,
  DateTime UploadDate,
  DateTime CurrentDate,
  DateTime? ExpirationDate,
  int? MaxDownloads
);