using GhostSend.Application.Files.Commands.UploadFile;
using GhostSend.Application.Files.Queries.GetFileMetadata;
using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace GhostSend.UnitTests.Application;

public class GetFileMetadataHandleTest
{
    [Fact]
    public async Task GetFileMetadataCorrectly()
    {
        var storedFileMock = new Mock<StoredFile>();
        var repositoryMock = new Mock<IFileRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider();
        var storageMock = new Mock<IStorageService>();
        StoredFile fileSaved = null!;

        storageMock.Setup(x => x.SaveAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync("uploads/test-path-data");

        repositoryMock.Setup(x => x.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
                .Callback<StoredFile, CancellationToken>((file, cancellationToken) =>
                {
                    fileSaved = file;
                });

        var handlerUploadFile = new UploadFileCommandHandler(repositoryMock.Object, storageMock.Object, unitOfWorkMock.Object, timeProvider);

        var command = new UploadFileCommand(
            new MemoryStream([1, 2, 3]), "test.txt", "text/plain", 1, 1, TimeSpan.FromDays(1)
        );

        var resultUploadFile = await handlerUploadFile.Handle(command, CancellationToken.None);

        StoredFile? storedFile = await repositoryMock.Object.GetByIdAsync(resultUploadFile.FileId, CancellationToken.None);

        var handlerGetFileMetadata = new GetFileMetadataQueryHandler(repositoryMock.Object);

        var query = new GetFileMetadataQuery(storedFile!.Id);

        var resultGetFileMetadata = await handlerGetFileMetadata.Handle(query, CancellationToken.None);

        Assert.Equal(storedFile.Id, resultGetFileMetadata.Id);
        Assert.Equal(storedFile.FileName, resultGetFileMetadata.FileName);
        Assert.Equal(storedFile.ContentType, resultGetFileMetadata.ContentType);
        Assert.Equal(storedFile.Size, resultGetFileMetadata.Size);
        Assert.Equal(storedFile.MaxDownloads, resultGetFileMetadata.MaxDownloads);
        Assert.Equal(storedFile.ExpirationDate, resultGetFileMetadata.ExpirationDate);
    }
}
