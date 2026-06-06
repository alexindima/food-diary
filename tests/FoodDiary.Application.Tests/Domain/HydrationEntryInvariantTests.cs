using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class HydrationEntryInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() => HydrationEntry.Create(UserId.Empty, DateTime.UtcNow, 250));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void Create_WithInvalidAmount_Throws(int amountMl) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HydrationEntry.Create(UserId.New(), DateTime.UtcNow, amountMl));
    }

    [Fact]
    public void Create_WithLocalTimestamp_NormalizesToUtc() {
        var localTimestamp = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

        var entry = HydrationEntry.Create(UserId.New(), localTimestamp, 250);

        Assert.Equal(DateTimeKind.Utc, entry.Timestamp.Kind);
    }

    [Fact]
    public void Update_WithSameValues_DoesNotSetModifiedOnUtc() {
        DateTime timestamp = DateTime.UtcNow;
        var entry = HydrationEntry.Create(UserId.New(), timestamp, 250);

        entry.Update(amountMl: 250, timestampUtc: timestamp);

        Assert.Null(entry.ModifiedOnUtc);
    }

    [Fact]
    public void Update_WithChangedValues_UpdatesState() {
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);
        DateTime nextTimestamp = DateTime.UtcNow.AddMinutes(5);

        entry.Update(amountMl: 500, timestampUtc: nextTimestamp);

        Assert.Equal(500, entry.AmountMl);
        Assert.Equal(nextTimestamp, entry.Timestamp);
        Assert.NotNull(entry.ModifiedOnUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void Update_WithInvalidAmount_Throws(int amountMl) {
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.Update(amountMl: amountMl));
    }
}
