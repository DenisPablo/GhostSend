using GhostSend.Application.Files.Queries.DownloadFile;
using GhostSend.Domain.Entities;
using GhostSend.Domain.Exceptions;
using GhostSend.Domain.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace GhostSend.UnitTests.Application;

public class DownloadFileQueryHandlerTests
{
    [Fact]
    public async Task DownloadPreExpirationFile()
    {
        var fileRepositoryMock = new Mock<IFileRepository>();
        var storageMock = new Mock<IStorageService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var stream = new MemoryStream();

        var fileId = Guid.NewGuid();

        var fileName = "document.pdf";
        var contentType = "application/pdf";
        var size = 1024;
        var maxDownloads = 1;
        var expirationDate = TimeSpan.FromHours(1);

        var storedFile = new StoredFile(fileName, contentType, size, maxDownloads, timeProvider, expirationDate);

        fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedFile);

        storageMock.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        var handler = new DownloadFileQueryHandler(
                                                    fileRepositoryMock.Object,
                                                    storageMock.Object,
                                                    unitOfWorkMock.Object,
                                                    timeProvider
                                                    );

        var query = new DownloadFileQuery(fileId);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(fileName, result.FileName);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(size, result.Size);

        Assert.Equal(1, storedFile.CurrentDownloads);

        fileRepositoryMock.Verify(x => x.UpdateAsync(storedFile, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadExpiredFile()
    {
        var fileRepositoryMock = new Mock<IFileRepository>();
        var storageMock = new Mock<IStorageService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var now = DateTimeOffset.UtcNow;
        var timeProvider = new FakeTimeProvider(now);
        var stream = new MemoryStream();

        var fileId = Guid.NewGuid();

        var fileName = "document.pdf";
        var contentType = "application/pdf";
        var size = 1024;
        var maxDownloads = 1;
        var expirationDate = TimeSpan.FromHours(1);

        var storedFile = new StoredFile(fileName, contentType, size, maxDownloads, timeProvider, expirationDate);

        timeProvider.Advance(TimeSpan.FromHours(2));

        fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedFile);

        storageMock.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        var handler = new DownloadFileQueryHandler(
                                                    fileRepositoryMock.Object,
                                                    storageMock.Object,
                                                    unitOfWorkMock.Object,
                                                    timeProvider
                                                    );

        var query = new DownloadFileQuery(fileId);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(query, CancellationToken.None));

        fileRepositoryMock.Verify(x => x.UpdateAsync(storedFile, It.IsAny<CancellationToken>()), Times.Never);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

}
