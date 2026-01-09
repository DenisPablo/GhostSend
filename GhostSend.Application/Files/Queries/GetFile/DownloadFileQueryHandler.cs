using GhostSend.Domain.Interfaces;
using MediatR;

namespace GhostSend.Application.Files.Queries.GetFile;


public class DownloadFileQueryHandler(IFileRepository fileRepository, IStorageService storageService, IUnitOfWork unitOfWork) : IRequestHandler<DownloadFileQuery, FileDownloadResponse>
{
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IStorageService _storageService = storageService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<FileDownloadResponse> Handle(DownloadFileQuery request, CancellationToken cancellationToken)
    {
        //Buscamos metadatos para saber la ruta fisica del archivo
        var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);

        if (file is not null)
        {
            file.IncrementDownloads();

            await _fileRepository.UpdateAsync(file, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var stream = await _storageService.GetAsync(file.Id, file.StoragePath, cancellationToken);

            return new FileDownloadResponse(stream, file.FileName, file.ContentType, file.Size);
        }

        throw new KeyNotFoundException($"File with id {request.FileId} not found");
    }
}
