using FluentAssertions;
using GhostSend.Domain.Entities;
using GhostSend.Domain.Interfaces;
using GhostSend.Infrastructure.BackgroundJobs;
using GhostSend.Infrastructure.Persistence;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GhostSend.UnitTests.Infrastructure;

public class FileCleanWorkerTest : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly Mock<IStorageService> _storageServiceMock;
    private IServiceProvider _services = null!;

    public FileCleanWorkerTest(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _storageServiceMock = new Mock<IStorageService>();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();

        _services = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IStorageService>();
                services.AddSingleton(_storageServiceMock.Object);
            });
        }).Services;
    }

    public async Task DisposeAsync()
    {
        // Clean database between tests
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.StoredFiles.RemoveRange(context.StoredFiles);
        await context.SaveChangesAsync();

        _storageServiceMock.Reset();
    }

    private async Task<List<StoredFile>> SeedFiles(params StoredFile[] files)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // RawVersion will be generated automatically by the database (xmin in PostgreSQL)
        context.StoredFiles.AddRange(files);
        await context.SaveChangesAsync();
        return files.ToList();
    }

    private StoredFile CreateExpiredFile()
    {
        var mockTime = new Mock<TimeProvider>();
        // Create file that was uploaded 2 hours ago with 1-hour lifetime (so expired 1 hour ago)
        mockTime.Setup(t => t.GetUtcNow()).Returns(DateTimeOffset.UtcNow.AddHours(-2));

        var file = new StoredFile(
            fileName: $"expired_{Guid.NewGuid()}.txt",
            contentType: "text/plain",
            size: 100,
            maxDownloads: null,
            timeProvider: mockTime.Object,
            lifeTime: TimeSpan.FromHours(1)
        );

        file.SetStoragePath($"/storage/expired_{file.Id}.txt");

        // Mark as expired by setting the IsExpired property using reflection
        var isExpiredProperty = file.GetType().GetProperty("IsExpired");
        isExpiredProperty?.SetValue(file, true);

        return file;
    }

    private StoredFile CreateValidFile()
    {
        var mockTime = new Mock<TimeProvider>();
        // Create file just now with 24-hour lifetime (won't expire for 24 hours)
        mockTime.Setup(t => t.GetUtcNow()).Returns(DateTimeOffset.UtcNow);

        var file = new StoredFile(
            fileName: $"valid_{Guid.NewGuid()}.txt",
            contentType: "text/plain",
            size: 200,
            maxDownloads: null,
            timeProvider: mockTime.Object,
            lifeTime: TimeSpan.FromHours(24)
        );

        file.SetStoragePath($"/storage/valid_{file.Id}.txt");

        return file;
    }

    private FileCleanWorker CreateWorker()
    {
        var scopeFactory = _services.GetRequiredService<IServiceScopeFactory>();
        var logger = _services.GetRequiredService<ILogger<FileCleanWorker>>();
        return new FileCleanWorker(scopeFactory, logger);
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_ShouldDeleteExpiredFiles_AndRemoveFromDatabase()
    {
        // Arrange
        var expiredFile = CreateExpiredFile();
        await SeedFiles(expiredFile);

        _storageServiceMock
            .Setup(s => s.DeleteAsync(expiredFile.StoragePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = CreateWorker();

        // Act
        await worker.CleanupExpiredFilesAsync();

        // Assert
        _storageServiceMock.Verify(
            s => s.DeleteAsync(expiredFile.StoragePath, It.IsAny<CancellationToken>()),
            Times.Once,
            "El archivo expirado debe ser eliminado del almacenamiento");

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dbFile = await context.StoredFiles.FindAsync(expiredFile.Id);

        dbFile.Should().BeNull("El archivo expirado debe ser eliminado de la base de datos");
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_ShouldNotDeleteValidFiles()
    {
        // Arrange
        var validFile = CreateValidFile();
        await SeedFiles(validFile);

        var worker = CreateWorker();

        // Act
        await worker.CleanupExpiredFilesAsync();

        // Assert
        _storageServiceMock.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "No debe eliminarse ningún archivo del almacenamiento");

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dbFile = await context.StoredFiles.FindAsync(validFile.Id);

        dbFile.Should().NotBeNull("El archivo válido debe permanecer en la base de datos");
        dbFile!.FileName.Should().Be(validFile.FileName);
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_WithMixedFiles_ShouldDeleteOnlyExpiredOnes()
    {
        // Arrange
        var expiredFile1 = CreateExpiredFile();
        var validFile1 = CreateValidFile();
        var expiredFile2 = CreateExpiredFile();
        var validFile2 = CreateValidFile();

        await SeedFiles(expiredFile1, validFile1, expiredFile2, validFile2);

        _storageServiceMock
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = CreateWorker();

        // Act
        await worker.CleanupExpiredFilesAsync();

        // Assert
        _storageServiceMock.Verify(
            s => s.DeleteAsync(expiredFile1.StoragePath, It.IsAny<CancellationToken>()),
            Times.Once,
            "El primer archivo expirado debe ser eliminado");

        _storageServiceMock.Verify(
            s => s.DeleteAsync(expiredFile2.StoragePath, It.IsAny<CancellationToken>()),
            Times.Once,
            "El segundo archivo expirado debe ser eliminado");

        _storageServiceMock.Verify(
            s => s.DeleteAsync(validFile1.StoragePath, It.IsAny<CancellationToken>()),
            Times.Never,
            "El primer archivo válido NO debe ser eliminado");

        _storageServiceMock.Verify(
            s => s.DeleteAsync(validFile2.StoragePath, It.IsAny<CancellationToken>()),
            Times.Never,
            "El segundo archivo válido NO debe ser eliminado");

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        (await context.StoredFiles.FindAsync(expiredFile1.Id))
            .Should().BeNull("El primer archivo expirado debe eliminarse de la BD");
        (await context.StoredFiles.FindAsync(expiredFile2.Id))
            .Should().BeNull("El segundo archivo expirado debe eliminarse de la BD");
        (await context.StoredFiles.FindAsync(validFile1.Id))
            .Should().NotBeNull("El primer archivo válido debe permanecer en la BD");
        (await context.StoredFiles.FindAsync(validFile2.Id))
            .Should().NotBeNull("El segundo archivo válido debe permanecer en la BD");
    }

    [Fact]
    public async Task CleanupExpiredFilesAsync_WhenStorageDeletionFails_ShouldNotRemoveFromDatabase()
    {
        // Arrange
        var expiredFile1 = CreateExpiredFile(); // Éxito
        var expiredFile2 = CreateExpiredFile(); // Fallo
        var expiredFile3 = CreateExpiredFile(); // Éxito

        await SeedFiles(expiredFile1, expiredFile2, expiredFile3);

        // Configurar el mock: file2 falla, otros tienen éxito
        _storageServiceMock
            .Setup(s => s.DeleteAsync(expiredFile2.StoragePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Error al eliminar archivo del almacenamiento"));

        _storageServiceMock
            .Setup(s => s.DeleteAsync(expiredFile1.StoragePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _storageServiceMock
            .Setup(s => s.DeleteAsync(expiredFile3.StoragePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = CreateWorker();

        // Act
        await worker.CleanupExpiredFilesAsync();

        // Assert - Verificar que se intentó eliminar los 3 archivos
        _storageServiceMock.Verify(
            s => s.DeleteAsync(expiredFile1.StoragePath, It.IsAny<CancellationToken>()),
            Times.Once);
        _storageServiceMock.Verify(
            s => s.DeleteAsync(expiredFile2.StoragePath, It.IsAny<CancellationToken>()),
            Times.Once);
        _storageServiceMock.Verify(
            s => s.DeleteAsync(expiredFile3.StoragePath, It.IsAny<CancellationToken>()),
            Times.Once);

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // File1 y File3 deben eliminarse de la BD porque el almacenamiento se eliminó correctamente
        var dbFile1 = await context.StoredFiles.FindAsync(expiredFile1.Id);
        var dbFile3 = await context.StoredFiles.FindAsync(expiredFile3.Id);

        dbFile1.Should().BeNull(
            "El archivo 1 debe eliminarse de la BD porque su eliminación del almacenamiento fue exitosa");
        dbFile3.Should().BeNull(
            "El archivo 3 debe eliminarse de la BD porque su eliminación del almacenamiento fue exitosa");

        // File2 debe permanecer en la BD porque la eliminación del almacenamiento falló
        var dbFile2 = await context.StoredFiles.FindAsync(expiredFile2.Id);
        dbFile2.Should().NotBeNull(
            "El archivo 2 debe permanecer en la BD porque la eliminación del almacenamiento falló");
        dbFile2!.FileName.Should().Contain("expired",
            "El archivo que permanece debe ser el archivo expirado que no se pudo eliminar");
    }
}
