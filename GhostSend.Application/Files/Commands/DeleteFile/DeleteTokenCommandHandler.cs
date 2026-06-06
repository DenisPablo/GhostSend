using GhostSend.Application.Common.Errors;
using GhostSend.Application.Common.Exceptions;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Exceptions;
using GhostSend.Domain.Interfaces;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace GhostSend.Application.Files.Commands.DeleteFile;

public class DeleteTokenCommandHandler(IFileRepository fileRepository, IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<DeleteFileCommand>
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

            // Mark the file as expired to delegate physical and DB deletion to the background Clean Worker
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                file.MarkExpired();
                await fileRepository.UpdateAsync(file, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);
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
