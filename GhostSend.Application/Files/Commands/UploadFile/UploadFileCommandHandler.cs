using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using MediatR;

namespace GhostSend.Application.Files.Commands.UploadFile;

public class UploadFileCommandHandler(IFileRepository fileRepository, IStorageService storageService) : IRequestHandler<UploadFileCommand, Guid>
{
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IStorageService _storageService = storageService;

    public async Task<Guid> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var storedFile = new StoredFile
                                        (
                                            request.FileName,
                                            request.ContentType,
                                            request.Size,
                                            DateTime.UtcNow,
                                            request.MaxDownloads,
                                            request.LifeTime
                                        );
        var path = await _storageService.SaveAsync(request.Stream, storedFile.Id, cancellationToken);

        storedFile.SetStoragePath(path);

        await _fileRepository.UploadAsync(storedFile, cancellationToken);

        return storedFile.Id;
    }
}