using GhostSend.Application.Files.Commands.UploadFile;

namespace GhostSend.Api.DTOs;

public record UploadFileRequest(
    IFormFile File,
    int? MaxDownloads,
    int? LifeTimeDays
)
{
    public UploadFileCommand ToCommand() => new(
        File.OpenReadStream(),
        File.FileName,
        File.ContentType,
        File.Length,
        MaxDownloads,
        LifeTimeDays.HasValue ? TimeSpan.FromDays(LifeTimeDays.Value) : null
    );
}