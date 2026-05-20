using GhostSend.Application.Common.Errors;
using GhostSend.Application.Common.Exceptions;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Exceptions;
using GhostSend.Domain.Interfaces;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace GhostSend.Application.Files.Commands.DeleteFile;

public class DeleteTokenCommandHandler(IFileRepository fileRepository, IStorageService storageService, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<DeleteFileCommand>
{
    public async Task Handle(DeleteFileCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var file = await fileRepository.GetByIdAsync(command.FileId, cancellationToken) ?? throw new ApplicationLayerException(ApplicationErrors.Files.FileNotFound);

            if (file.IsExpiredAt(timeProvider.GetUtcNow().UtcDateTime))
            {
                throw new ValidationException(new Dictionary<string, string[]> { { "File", [DomainErrors.StoredFile.FileExpired] } });
            }

            // Use constant-time comparison to prevent timing attacks on the delete token.
            var commandTokenBytes = Encoding.UTF8.GetBytes(command.DeleteToken ?? string.Empty);
            var storedTokenBytes = Encoding.UTF8.GetBytes(file.DeleteToken);

            if (!CryptographicOperations.FixedTimeEquals(commandTokenBytes, storedTokenBytes))
            {
                throw new ApplicationLayerException(ApplicationErrors.Files.InvalidDeleteToken);
            }

            var storagePath = file.StoragePath;

            // Delete physically from Storage (network call outside database transaction)
            await storageService.DeleteAsync(storagePath, cancellationToken);

            // Delete from Database
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
