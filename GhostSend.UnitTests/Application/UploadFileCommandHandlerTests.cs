using GhostSend.Application.Files.Commands.UploadFile;
using GhostSend.Domain.Entities;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Exceptions;
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
            new MemoryStream([1, 2, 3]), "test.txt", "text/plain", 1, 1, TimeSpan.FromDays(1)
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

    [Fact]
    public async Task UploadFileCommandHandler_FileRequired()
    {
        var storageMock = new Mock<IStorageService>();
        var repositoryMock = new Mock<IFileRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider();

        var handler = new UploadFileCommandHandler(repositoryMock.Object, storageMock.Object, unitOfWorkMock.Object, timeProvider);

        var command = new UploadFileCommand(
            null!, "test.txt", "text/plain", 1, 1, TimeSpan.FromDays(1)
        );

        await Assert.ThrowsAsync<ValidationException>(async () => await handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UploadFileCommandHandler_WhenFileExceedsMaxSize()
    {
        var storageMock = new Mock<IStorageService>();
        var repositoryMock = new Mock<IFileRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider();

        var gianStreamMock = new Mock<Stream>();

        long limitPlusOne = StoredFile.MaxSize + 1;
        gianStreamMock.Setup(x => x.Length).Returns(limitPlusOne);

        var handler = new UploadFileCommandHandler(
            repositoryMock.Object,
            storageMock.Object,
            unitOfWorkMock.Object,
            timeProvider
        );

        var command = new UploadFileCommand(
            gianStreamMock.Object,
            "video_pesado.mp4",
            "video/mp4",
            1,
            1,
            TimeSpan.FromDays(1)
        );

        var exception = await Assert.ThrowsAsync<ValidationException>(async () => await handler.Handle(command, CancellationToken.None));

        Assert.Contains(DomainErrors.StoredFile.FileTooLarge, exception.Errors["Size"]);

        storageMock.Verify(x => x.SaveAsync(It.IsAny<Stream>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        repositoryMock.Verify(x => x.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);

        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenFileIsExactlyMaxSize()
    {
        var storageMock = new Mock<IStorageService>();
        var repositoryMock = new Mock<IFileRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider();

        storageMock.Setup(x => x.SaveAsync(It.IsAny<Stream>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("uploads/test-path-data");

        var borderLimitStreamMock = new Mock<Stream>();

        borderLimitStreamMock.Setup(x => x.Length).Returns(StoredFile.MaxSize);

        var handler = new UploadFileCommandHandler(
            repositoryMock.Object,
            storageMock.Object,
            unitOfWorkMock.Object,
            timeProvider
        );

        var command = new UploadFileCommand(
            borderLimitStreamMock.Object,
            "video_pesado.mp4",
            "video/mp4",
            1,
            1,
            TimeSpan.FromDays(1)
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(result, Guid.Empty);
        repositoryMock.Verify(x => x.AddAsync(It.Is<StoredFile>(f => f.Size == StoredFile.MaxSize), It.IsAny<CancellationToken>()), Times.Once);
    }
}