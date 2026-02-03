using GhostSend.Application.Common.Errors;
using GhostSend.Application.Common.Exceptions;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Exceptions;
using GhostSend.Domain.Interfaces;
using MediatR;

namespace GhostSend.Application.Files.Commands.DeleteFile;

public class DeleteTokenCommandHandler(IFileRepository fileRepository, IUnitOfWork unitOfWork, IStorageService storageService) : IRequestHandler<DeleteFileCommand>
{
    public async Task Handle(DeleteFileCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var file = await fileRepository.GetByIdAsync(command.FileId, cancellationToken) ?? throw new ApplicationLayerException(ApplicationErrors.Files.FileNotFound);

            if (file.DeleteToken != command.DeleteToken)
            {
                throw new ApplicationLayerException(ApplicationErrors.Files.InvalidDeleteToken);
            }

            await storageService.DeleteAsync(file.StoragePath, cancellationToken);
            await fileRepository.DeleteAsync(file, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (BaseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationLayerException(ApplicationErrors.Files.DeleteError, ex);
        }
    }
}