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
public class UploadFileCommandHandler(IFileRepository fileRepository, IStorageService storageService, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<UploadFileCommand, Guid>
{
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IStorageService _storageService = storageService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>
    /// Validates the request, saves the file to storage, and persists metadata to the database.
    /// </summary>
    public async Task<Guid> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        if (request.Stream == null)
        {
            throw new ValidationException(new Dictionary<string, string[]> { { "File", [DomainErrors.StoredFile.FileRequired] } });
        }
        try
        {

            var size = request.Stream.Length;

            var storedFile = new StoredFile(
                                                request.FileName,
                                                request.ContentType,
                                                size,
                                                request.MaxDownloads,
                                                _timeProvider,
                                                request.LifeTime
                                            );

            var storagePath = await _storageService.SaveAsync(request.Stream, storedFile.Id, cancellationToken);

            storedFile.SetStoragePath(storagePath);

            await _fileRepository.AddAsync(storedFile, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return storedFile.Id;
        }
        catch (BaseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationLayerException(ApplicationErrors.Files.UploadError, ex);
        }
    }
}