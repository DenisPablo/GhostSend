using GhostSend.Domain.Entities;
using Microsoft.Extensions.Time.Testing;

namespace GhostSend.UnitTests.Domain;

public class StoredFileTests
{

    [Fact]
    public void IsExpired_MaxDownloads()
    {
        var storedFile = new StoredFile("test.txt", "text/plain", 100, 1, TimeProvider.System, null);

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
}