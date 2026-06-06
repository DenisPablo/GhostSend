using GhostSend.Application.Common.Errors;
using GhostSend.Application.Common.Exceptions;
using GhostSend.Application.Files.Commands.DeleteFile;
using GhostSend.Domain.Entities;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Exceptions;
using GhostSend.Domain.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace GhostSend.UnitTests.Application;

public class DeleteFileCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMarkFileAsExpired_WhenTokenIsValid()
    {
        var fileRepositoryMock = new Mock<IFileRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider();

        var storedFile = new StoredFile("test.txt", "text/plain", 100, 5, timeProvider, TimeSpan.FromHours(1));
        var deleteToken = storedFile.DeleteToken;
        var fileId = storedFile.Id;

        fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedFile);

        unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        var handler = new DeleteTokenCommandHandler(fileRepositoryMock.Object, unitOfWorkMock.Object, timeProvider);
        var command = new DeleteFileCommand(fileId, deleteToken);

        await handler.Handle(command, CancellationToken.None);

        Assert.True(storedFile.IsExpired);
        fileRepositoryMock.Verify(x => x.UpdateAsync(storedFile, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTokenIsInvalid()
    {
        var fileRepositoryMock = new Mock<IFileRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider();

        var storedFile = new StoredFile("test.txt", "text/plain", 100, 5, timeProvider, TimeSpan.FromHours(1));
        var fileId = storedFile.Id;

        fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedFile);

        var handler = new DeleteTokenCommandHandler(fileRepositoryMock.Object, unitOfWorkMock.Object, timeProvider);
        var command = new DeleteFileCommand(fileId, "invalid-token");

        var exception = await Assert.ThrowsAsync<ApplicationLayerException>(() => handler.Handle(command, CancellationToken.None));

        Assert.Equal(ApplicationErrors.Files.InvalidDeleteToken, exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenFileNotFound()
    {
        var fileRepositoryMock = new Mock<IFileRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider();
        var fileId = Guid.NewGuid();

        fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredFile?)null);

        var handler = new DeleteTokenCommandHandler(fileRepositoryMock.Object, unitOfWorkMock.Object, timeProvider);
        var command = new DeleteFileCommand(fileId, "some-token");

        var exception = await Assert.ThrowsAsync<ApplicationLayerException>(() => handler.Handle(command, CancellationToken.None));

        Assert.Equal(ApplicationErrors.Files.FileNotFound, exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenFileIsAlreadyExpired()
    {
        var fileRepositoryMock = new Mock<IFileRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);

        var storedFile = new StoredFile("test.txt", "text/plain", 100, 5, timeProvider, TimeSpan.FromHours(1));
        var deleteToken = storedFile.DeleteToken;
        var fileId = storedFile.Id;

        timeProvider.Advance(TimeSpan.FromHours(2));
        storedFile.MarkExpired();

        fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedFile);

        var handler = new DeleteTokenCommandHandler(fileRepositoryMock.Object, unitOfWorkMock.Object, timeProvider);
        var command = new DeleteFileCommand(fileId, deleteToken);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));

        Assert.Contains(DomainErrors.StoredFile.FileExpired, exception.Errors["File"]);
    }
}