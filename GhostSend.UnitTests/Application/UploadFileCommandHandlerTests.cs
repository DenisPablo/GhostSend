using System.Threading.Tasks;
using GhostSend.Application.Files.Commands.UploadFile;
using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace GhostSend.UnitTests.Application;

public class UploadFileCommandHandlerTests
{
    [Fact]
    public async Task UploadFileCommandHandler()
    {
        var storageMock = new Mock<IStorageService>();
        var repositoryMock = new Mock<IFileRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider();
        StoredFile fileSaved = null!;


        storageMock.Setup(x => x.SaveAsync(It.IsAny<Stream>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("uploads/test-path-data");

        repositoryMock.Setup(x => x.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
                .Callback<StoredFile, CancellationToken>((file, cancellationToken) =>
                {
                    fileSaved = file;
                });

        var handler = new UploadFileCommandHandler(repositoryMock.Object, storageMock.Object, unitOfWorkMock.Object, timeProvider);

        var command = new UploadFileCommand(
            new MemoryStream(new byte[] { 1, 2, 3 }), "test.txt", "text/plain", 1, 1, TimeSpan.FromDays(1)
        );

        var result = await handler.Handle(command, CancellationToken.None);

        storageMock.Verify(x => x.SaveAsync(It.IsAny<Stream>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        repositoryMock.Verify(x => x.AddAsync(It.Is<StoredFile>(
            f => f.FileName == command.FileName &&
            f.ContentType == command.ContentType &&
            f.Size == command.Stream.Length &&
            f.MaxDownloads == command.MaxDownloads
            ), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(fileSaved.Id, result);
    }
}