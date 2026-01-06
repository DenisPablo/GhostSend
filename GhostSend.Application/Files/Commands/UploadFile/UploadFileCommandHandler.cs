using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using MediatR;

namespace GhostSend.Application.Files.Commands.UploadFile;

public class UploadFileCommandHandler(IFileRepository fileRepository, IStorageService storageService, IUnitOfWork unitOfWork) : IRequestHandler<UploadFileCommand, Guid>
{
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IStorageService _storageService = storageService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Guid> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var storedFile = new StoredFile(
                                            request.FileName,
                                            request.ContentType,
                                            request.Size,
                                            request.MaxDownloads,
                                            request.LifeTime
                                        );

        var storagePath = await _storageService.SaveAsync(request.Stream, storedFile.Id, cancellationToken);

        storedFile.SetStoragePath(storagePath);

        await _fileRepository.UploadAsync(storedFile, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return storedFile.Id;
    }
}