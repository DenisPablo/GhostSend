using GhostSend.Application.Files.Commands.UploadFile;

namespace GhostSend.Api.DTOs.Requests;

public class UploadFileRequest
{
    public IFormFile File { get; set; } = null!;
    public int? MaxDownloads { get; set; }
    public string? LifeTime { get; set; }

    public UploadFileCommand ToCommand()
    {
        TimeSpan? parsedLifeTime = null;
        if (!string.IsNullOrWhiteSpace(LifeTime) && TimeSpan.TryParse(LifeTime, out var ts))
        {
            parsedLifeTime = ts;
        }

        return new UploadFileCommand(
            File?.OpenReadStream() ?? Stream.Null,
            File?.FileName ?? "unknown",
            File?.ContentType ?? "application/octet-stream",
            File?.Length ?? 0,
            MaxDownloads,
            parsedLifeTime
        );
    }
}