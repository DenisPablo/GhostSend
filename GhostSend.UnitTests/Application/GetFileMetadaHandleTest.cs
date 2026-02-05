using GhostSend.Application.Files.Queries.GetFileMetadata;
using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace GhostSend.UnitTests.Application;

public class GetFileMetadataHandleTest
{
    [Fact]
    public async Task Handle_ShouldReturnMetadata_WhenFileExists()
    {
        var repositoryMock = new Mock<IFileRepository>();
        var timeProvider = new FakeTimeProvider();

        // Use StoredFile which is the actual domain entity
        var storedFile = new StoredFile(
            "test.txt",
            "text/plain",
            1024,
            5,
            timeProvider,
            TimeSpan.FromMinutes(10)
        );
        var fileId = storedFile.Id;

        repositoryMock.Setup(r => r.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedFile);

        var handler = new GetFileMetadataQueryHandler(repositoryMock.Object);
        var query = new GetFileMetadataQuery(fileId);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(storedFile.Id, result.Id);
        Assert.Equal(storedFile.FileName, result.FileName);
        Assert.Equal(storedFile.Size, result.Size);
        Assert.Equal(storedFile.ContentType, result.ContentType);
        Assert.Equal(storedFile.UploadDate, result.UploadDate);
        Assert.Equal(storedFile.MaxDownloads, result.MaxDownloads);
    }
}