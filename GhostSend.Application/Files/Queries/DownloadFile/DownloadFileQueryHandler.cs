using GhostSend.Application.Common.Errors;
using GhostSend.Application.Common.Exceptions;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Exceptions;
using GhostSend.Domain.Interfaces;
using MediatR;

namespace GhostSend.Application.Files.Queries.DownloadFile;

/// <summary>
/// Handles the request to download a file, verifying expiration and retrieving the stream.
/// </summary>
public class DownloadFileQueryHandler(IFileRepository fileRepository, IStorageService storageService, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<DownloadFileQuery, FileDownloadResponse>
{
    /// <summary>
    /// Processes the download query by checking expiration logic and fetching file content.
    /// </summary>
    public async Task<FileDownloadResponse> Handle(DownloadFileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var file = await fileRepository.GetByIdAsync(request.FileId, cancellationToken) ??
                throw new NotFoundException("File", request.FileId);

            file.Download(timeProvider.GetUtcNow().UtcDateTime);

            await fileRepository.UpdateAsync(file, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var stream = await storageService.GetAsync(file.StoragePath, cancellationToken);

            return new FileDownloadResponse(stream, file.FileName, file.ContentType, file.Size);
        }
        catch (ConcurrencyException)
        {
            throw new GhostSend.Domain.Exceptions.ValidationException(new Dictionary<string, string[]> { { "File", [DomainErrors.StoredFile.FileExpired] } });
        }
        catch (BaseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationLayerException(ApplicationErrors.Files.DownloadError, ex);
        }
    }
}
