using System.Reflection.Metadata;
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

        var fileId = Guid.NewGuid;

        var timeProvider = new FakeTimeProvider();
        
        var expectedMetadata = new FileMetadata
        {
            Id = fileId,
            Name = "test.txt",
            Size = 1024,
            ContentType = "text/plain",
            UploadDate = DateTime.UtcNow
        };

        repositoryMock.Setup(r => r.GetMetadataAsync(fileId))
            .ReturnsAsync(expectedMetadata);

        var handler = new GetFileMetadataHandler(repositoryMock.Object);
        var query = new GetFileMetadataQuery(fileId);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedMetadata.Id, result.Id);
        Assert.Equal(expectedMetadata.Name, result.Name);
        Assert.Equal(expectedMetadata.Size, result.Size);
        Assert.Equal(expectedMetadata.ContentType, result.ContentType);
        Assert.Equal(expectedMetadata.UploadDate, result.UploadDate);
    }
}