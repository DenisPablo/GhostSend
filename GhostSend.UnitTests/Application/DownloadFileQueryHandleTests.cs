using GhostSend.Application.Files.Queries.DownloadFile;
using GhostSend.Domain.Entities;
using GhostSend.Domain.Errors;
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

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenFileDoesNotExist()
    {
        var fileRepositoryMock = new Mock<IFileRepository>();
        var storageMock = new Mock<IStorageService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider();
        var fileId = Guid.NewGuid();

        fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredFile?)null);

        var handler = new DownloadFileQueryHandler(
            fileRepositoryMock.Object,
            storageMock.Object,
            unitOfWorkMock.Object,
            timeProvider);

        var query = new DownloadFileQuery(fileId);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldReturnConcurrentDownloadMessage_WhenConcurrencyConflictOccursAndFileIsStillValid()
    {
        var fileRepositoryMock = new Mock<IFileRepository>();
        var storageMock = new Mock<IStorageService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var stream = new MemoryStream();

        var fileId = Guid.NewGuid();
        var storedFile = new StoredFile("doc.pdf", "application/pdf", 1024, 10, timeProvider, TimeSpan.FromHours(1));
        var fileAfterReload = new StoredFile("doc.pdf", "application/pdf", 1024, 10, timeProvider, TimeSpan.FromHours(1));

        fileRepositoryMock.SetupSequence(x => x.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedFile)   // first read
            .ReturnsAsync(fileAfterReload); // reload after concurrency

        storageMock.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyException("Conflict"));

        var handler = new DownloadFileQueryHandler(
            fileRepositoryMock.Object,
            storageMock.Object,
            unitOfWorkMock.Object,
            timeProvider);

        var query = new DownloadFileQuery(fileId);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(query, CancellationToken.None));

        Assert.Contains(DomainErrors.StoredFile.ConcurrentDownload, exception.Errors["File"]);
    }

    [Fact]
    public async Task Handle_ShouldReturnExpiredMessage_WhenConcurrencyConflictOccursAndFileIsActuallyExpired()
    {
        var fileRepositoryMock = new Mock<IFileRepository>();
        var storageMock = new Mock<IStorageService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var stream = new MemoryStream();

        var fileId = Guid.NewGuid();
        var storedFile = new StoredFile("doc.pdf", "application/pdf", 1024, 1, timeProvider, TimeSpan.FromHours(1));
        var fileAfterReload = new StoredFile("doc.pdf", "application/pdf", 1024, 1, timeProvider, TimeSpan.FromHours(1));

        // Simulate that after reload, the file is expired (another download exhausted it)
        fileAfterReload.MarkExpired();

        fileRepositoryMock.SetupSequence(x => x.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedFile)   // first read
            .ReturnsAsync(fileAfterReload); // reload after concurrency

        storageMock.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyException("Conflict"));

        var handler = new DownloadFileQueryHandler(
            fileRepositoryMock.Object,
            storageMock.Object,
            unitOfWorkMock.Object,
            timeProvider);

        var query = new DownloadFileQuery(fileId);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(query, CancellationToken.None));

        Assert.Contains(DomainErrors.StoredFile.FileExpired, exception.Errors["File"]);
    }
}
