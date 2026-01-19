using GhostSend.Domain.Entities;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Exceptions;
using Microsoft.Extensions.Time.Testing;
namespace GhostSend.UnitTests.Domain;

public class StoredFileTests
{
    [Fact]
    public void Constructor()
    {
        var initDate = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(initDate);
        var lifeTime = TimeSpan.FromHours(1);

        var storedFile = new StoredFile("test.txt", "text/plain", 100, 1, fakeTime, lifeTime);

        var expectedExpirationDate = initDate.DateTime.Add(lifeTime);

        Assert.NotEqual(Guid.Empty, storedFile.Id);
        Assert.Equal("test.txt", storedFile.FileName);
        Assert.Equal("text/plain", storedFile.ContentType);
        Assert.Equal(100, storedFile.Size);
        Assert.Equal(1, storedFile.MaxDownloads);
        Assert.Equal(expectedExpirationDate, storedFile.ExpirationDate);
        Assert.Equal(0, storedFile.CurrentDownloads);
    }

    [Fact]
    public void problematicConstructor()
    {
        var invalidName = "";
        var invalidContentType = "";
        var invalidSize = 0;
        var invalidMaxDownloads = 0;
        var invalidLifeTime = TimeSpan.FromMinutes(-1);
        var fakeTime = new FakeTimeProvider(DateTimeOffset.Now);

        var ex = Assert.Throws<GhostSend.Domain.Exceptions.ValidationException>(() =>
        new StoredFile(invalidName, invalidContentType, invalidSize, invalidMaxDownloads, fakeTime, invalidLifeTime)
    );

        Assert.Contains(DomainErrors.StoredFile.FileNameRequired, ex.Errors["FileName"]);
        Assert.Contains(DomainErrors.StoredFile.ContentTypeRequired, ex.Errors["ContentType"]);
        Assert.Contains(DomainErrors.StoredFile.NegativeSize, ex.Errors["Size"]);
        Assert.Contains(DomainErrors.StoredFile.NegativeMaxDownloads, ex.Errors["MaxDownloads"]);
        Assert.Contains(DomainErrors.StoredFile.NegativeLifeTime, ex.Errors["LifeTime"]);
    }

    [Fact]
    public void Download_IncrementsCurrentDownloads()
    {
        var storedFile = new StoredFile("test.txt", "text/plain", 100, 1, TimeProvider.System, TimeSpan.FromHours(1));

        storedFile.Download(TimeProvider.System.GetUtcNow().DateTime);

        Assert.Equal(1, storedFile.CurrentDownloads);
    }

    [Fact]
    public void Download_ThrowsWhenMaxDownloadsReached()
    {
        var storedFile = new StoredFile("test.txt", "text/plain", 100, 1, TimeProvider.System, TimeSpan.FromHours(1));

        storedFile.Download(TimeProvider.System.GetUtcNow().DateTime);

        Assert.Throws<ValidationException>(() => storedFile.Download(TimeProvider.System.GetUtcNow().DateTime));
    }

    [Fact]
    public void Download_ThrowsWhenDateTimeExpired()
    {
        var initDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(initDate);

        var storedFile = new StoredFile("test.txt", "text/plain", 100, 1, fakeTime, TimeSpan.FromHours(1));

        fakeTime.Advance(TimeSpan.FromHours(2));

        var simulateTime = fakeTime.GetUtcNow().DateTime;

        Assert.Throws<ValidationException>(() => storedFile.Download(simulateTime));
    }

    [Fact]
    public void Download_DoesNotThrow_WhenValid()
    {
        var storedFile = new StoredFile("test.txt", "text/plain", 100, 2, TimeProvider.System, TimeSpan.FromHours(1));

        storedFile.Download(TimeProvider.System.GetUtcNow().DateTime);

        Assert.Equal(1, storedFile.CurrentDownloads);
    }
}