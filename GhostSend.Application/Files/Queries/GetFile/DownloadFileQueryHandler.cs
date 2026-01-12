using GhostSend.Domain.Exceptions;
using GhostSend.Domain.Interfaces;
using MediatR;

namespace GhostSend.Application.Files.Queries.GetFile;


public class DownloadFileQueryHandler(IFileRepository fileRepository, IStorageService storageService, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<DownloadFileQuery, FileDownloadResponse>
{
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IStorageService _storageService = storageService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<FileDownloadResponse> Handle(DownloadFileQuery request, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);

        if (file == null)
        {
            throw new NotFoundException("File", request.FileId);
        }

        if (file.IsExpired(_timeProvider.GetUtcNow().UtcDateTime))
        {
            throw new ConflictException("The file has expired.");
        }

        if (file.CurrentDownloads >= file.MaxDownloads)
        {
            throw new ConflictException("The maximum number of downloads has been reached.");
        }

        file.IncrementDownloads();

        await _fileRepository.UpdateAsync(file, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var stream = await _storageService.GetAsync(file.Id, file.StoragePath, cancellationToken);

        return new FileDownloadResponse(stream, file.FileName, file.ContentType, file.Size);
    }
}
