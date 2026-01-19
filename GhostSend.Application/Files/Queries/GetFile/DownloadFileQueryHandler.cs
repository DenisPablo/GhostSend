using GhostSend.Application.Common.Errors;
using GhostSend.Application.Common.Exceptions;
using GhostSend.Domain.Errors;
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
        try
        {
            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken) ??
                throw new NotFoundException("File", request.FileId);

            file.Download(_timeProvider.GetUtcNow().UtcDateTime);

            await _fileRepository.UpdateAsync(file, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var stream = await _storageService.GetAsync(file.Id, file.StoragePath, cancellationToken);

            return new FileDownloadResponse(stream, file.FileName, file.ContentType, file.Size);
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
