namespace GhostSend.Domain.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string anonymousFileName, string contentType, CancellationToken cancellationToken = default);

    Task<Stream> DownloadFileAsync(string anonymousFileName, CancellationToken cancellationToken = default);

    Task DeleteAsync(string anonymousFileName, CancellationToken cancellationToken = default);

    Task<List<string>> ListFilesAsync(CancellationToken cancellationToken = default);
}