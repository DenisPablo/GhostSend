using GhostSend.Application.Files.Queries.GetFileMetadata;
using GhostSend.Domain.Entities;
using GhostSend.Domain.Exceptions;
using GhostSend.Domain.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace GhostSend.UnitTests.Application;

public class GetFileMetadataQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMetadata_WhenFileExists()
    {
        // Arrange
        var fileRepositoryMock = new Mock<IFileRepository>();
        var now = DateTimeOffset.UtcNow;
        var timeProvider = new FakeTimeProvider(now);

        var fileId = Guid.NewGuid();
        var fileName = "test.txt";
        var contentType = "text/plain";
        var size = 100L;
        var maxDownloads = 10;
        var expirationDate = TimeSpan.FromDays(1);

        var storedFile = new StoredFile(fileName, contentType, size, maxDownloads, timeProvider, expirationDate);

        fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedFile);

        var handler = new GetFileMetadataQueryHandler(fileRepositoryMock.Object);
        var query = new GetFileMetadataQuery(fileId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(storedFile.Id, result.Id);
        Assert.Equal(fileName, result.FileName);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(size, result.Size);
        Assert.Equal(maxDownloads, result.MaxDownloads);
        Assert.Equal(0, result.CurrentDownloads);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var fileRepositoryMock = new Mock<IFileRepository>();
        var fileId = Guid.NewGuid();

        fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredFile?)null);

        var handler = new GetFileMetadataQueryHandler(fileRepositoryMock.Object);
        var query = new GetFileMetadataQuery(fileId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }
}