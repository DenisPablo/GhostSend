using GhostSend.Application.Common.Errors;
using GhostSend.Application.Common.Exceptions;
using GhostSend.Domain.Entities;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Exceptions;
using GhostSend.Domain.Interfaces;
using MediatR;

namespace GhostSend.Application.Files.Commands.UploadFile;

/// <summary>
/// Handles the upload process of a file, including validation, storage, and persistence.
/// </summary>
public class UploadFileCommandHandler(IFileRepository fileRepository, IStorageService storageService, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<UploadFileCommand, UploadFileResponse>
{
    /// <summary>
    /// Validates the request, saves the file to storage, and persists metadata to the database.
    /// </summary>
    public async Task<UploadFileResponse> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        if (request.Stream == null)
        {
            throw new ValidationException(new Dictionary<string, string[]> { { "File", [DomainErrors.StoredFile.FileRequired] } });
        }
        string? storagePath = null;
        try
        {
            var storedFile = new StoredFile(
                                                request.FileName,
                                                request.ContentType,
                                                request.Size,
                                                request.MaxDownloads,
                                                timeProvider,
                                                request.LifeTime
                                            );

            var extension = System.IO.Path.GetExtension(request.FileName);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".dat";
            }
            var anonymousFileName = $"{Guid.NewGuid()}{extension}";

            storagePath = await storageService.UploadFileAsync(request.Stream, anonymousFileName, request.ContentType, cancellationToken);

            storedFile.SetStoragePath(storagePath);

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await fileRepository.AddAsync(storedFile, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);

            return new UploadFileResponse { FileId = storedFile.Id, DeleteToken = storedFile.DeleteToken };
        }
        catch (BaseException)
        {
            if (!string.IsNullOrWhiteSpace(storagePath))
            {
                try
                {
                    await storageService.DeleteAsync(storagePath, CancellationToken.None);
                }
                catch
                {
                    // Best-effort cleanup to avoid orphaned files on failed DB operations.
                }
            }
            throw;
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(storagePath))
            {
                try
                {
                    await storageService.DeleteAsync(storagePath, CancellationToken.None);
                }
                catch
                {
                    // Best-effort cleanup to avoid orphaned files on failed DB operations.
                }
            }
            throw new ApplicationLayerException(ApplicationErrors.Files.UploadError, ex);
        }
    }
}
