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

        Assert.True(ex.Errors.ContainsKey("DomainValidation"));

        var listaErrores = ex.Errors["DomainValidation"];

        Assert.Equal(5, listaErrores.Length);
        Assert.Contains(listaErrores, e => e == DomainErrors.StoredFile.FileNameRequired);
        Assert.Contains(listaErrores, e => e == DomainErrors.StoredFile.ContentTypeRequired);
        Assert.Contains(listaErrores, e => e == DomainErrors.StoredFile.NegativeSize);
        Assert.Contains(listaErrores, e => e == DomainErrors.StoredFile.NegativeMaxDownloads);
        Assert.Contains(listaErrores, e => e == DomainErrors.StoredFile.NegativeLifeTime);
    }

    [Fact]
    public void IncrementDownloads()
    {
        var storedFile = new StoredFile("test.txt", "text/plain", 100, 1, TimeProvider.System, TimeSpan.FromHours(1));

        storedFile.IncrementDownloads();

        Assert.Equal(1, storedFile.CurrentDownloads);
    }

    // Tests IsExpired
    [Fact]
    public void IsExpired_MaxDownloads()
    {
        var storedFile = new StoredFile("test.txt", "text/plain", 100, 1, TimeProvider.System, TimeSpan.FromHours(1));

        storedFile.IncrementDownloads();

        Assert.True(storedFile.IsExpired(TimeProvider.System.GetUtcNow().DateTime));
    }

    [Fact]
    public void IsExpired_DateTimeExpired()
    {

        var initDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(initDate);

        var storedFile = new StoredFile("test.txt", "text/plain", 100, 1, fakeTime, TimeSpan.FromHours(1));

        fakeTime.Advance(TimeSpan.FromHours(2));

        var simulateTime = fakeTime.GetUtcNow().DateTime;
        var result = storedFile.IsExpired(simulateTime);

        Assert.True(result);
    }

    [Fact]
    public void NoIsExpired()
    {
        var storedFile = new StoredFile("test.txt", "text/plain", 100, 1, TimeProvider.System, TimeSpan.FromHours(1));

        Assert.False(storedFile.IsExpired(TimeProvider.System.GetUtcNow().DateTime));
    }

    [Fact]
    public void NoIsExpired_MaxDownloads()
    {
        var storedFile = new StoredFile("test.txt", "text/plain", 100, 2, TimeProvider.System, TimeSpan.FromHours(1));

        storedFile.IncrementDownloads();

        Assert.False(storedFile.IsExpired(TimeProvider.System.GetUtcNow().DateTime));
    }
}