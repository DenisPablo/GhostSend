using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using MediatR;

namespace GhostSend.Application.Files.Commands.UploadFile;

public class UploadFileCommandHandler(IFileRepository fileRepository, IStorageService storageService, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<UploadFileCommand, Guid>
{
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IStorageService _storageService = storageService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Guid> Handle(UploadFileCommand request, CancellationToken cancellationToken)
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
}