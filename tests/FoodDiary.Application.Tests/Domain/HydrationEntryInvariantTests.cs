using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class HydrationEntryInvariantTests
{
    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => HydrationEntry.Create(UserId.Empty, DateTime.UtcNow, 250));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void Create_WithInvalidAmount_Throws(int amountMl)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HydrationEntry.Create(UserId.New(), DateTime.UtcNow, amountMl));
    }

    [Fact]
    public void Create_WithLocalTimestamp_NormalizesToUtc()
    {
        var localTimestamp = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

        var entry = HydrationEntry.Create(UserId.New(), localTimestamp, 250);

        Assert.Equal(DateTimeKind.Utc, entry.Timestamp.Kind);
    }

    [Fact]
    public void Update_WithSameValues_DoesNotSetModifiedOnUtc()
    {
        var timestamp = DateTime.UtcNow;
        var entry = HydrationEntry.Create(UserId.New(), timestamp, 250);

        entry.Update(amountMl: 250, timestampUtc: timestamp);

        Assert.Null(entry.ModifiedOnUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void Update_WithInvalidAmount_Throws(int amountMl)
    {
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.Update(amountMl: amountMl));
    }
}
