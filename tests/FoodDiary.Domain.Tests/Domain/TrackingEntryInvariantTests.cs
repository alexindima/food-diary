using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class TrackingEntryInvariantTests {
    [Fact]
    public void HydrationEntry_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            HydrationEntry.Create(UserId.Empty, DateTime.UtcNow, 250));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void HydrationEntry_Create_WithInvalidAmount_Throws(int amountMl) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HydrationEntry.Create(UserId.New(), DateTime.UtcNow, amountMl));
    }

    [Fact]
    public void HydrationEntry_Create_WithLocalTimestamp_NormalizesToUtc() {
        var localTimestamp = new DateTime(2026, 3, 27, 14, 30, 0, DateTimeKind.Local);

        var entry = HydrationEntry.Create(UserId.New(), localTimestamp, 250);

        Assert.Multiple(
            () => Assert.Equal(localTimestamp.ToUniversalTime(), entry.Timestamp),
            () => Assert.Equal(250, entry.AmountMl),
            () => Assert.NotEqual(HydrationEntryId.Empty, entry.Id));
    }

    [Fact]
    public void HydrationEntry_Update_WithSameValues_DoesNotSetModifiedOnUtc() {
        DateTime timestamp = DateTime.UtcNow;
        var entry = HydrationEntry.Create(UserId.New(), timestamp, 250);

        entry.Update(amountMl: 250, timestampUtc: timestamp);

        Assert.Null(entry.ModifiedOnUtc);
    }

    [Fact]
    public void HydrationEntry_Update_WithDifferentValues_SetsModifiedOnUtc() {
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);
        DateTime newTimestamp = DateTime.UtcNow.AddMinutes(5);

        entry.Update(amountMl: 500, timestampUtc: newTimestamp);

        Assert.Equal(500, entry.AmountMl);
        Assert.Equal(newTimestamp, entry.Timestamp);
        Assert.NotNull(entry.ModifiedOnUtc);
    }

    [Fact]
    public void HydrationEntry_Update_WithLocalTimestamp_NormalizesToUtc() {
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);
        var localTimestamp = new DateTime(2026, 3, 27, 14, 30, 0, DateTimeKind.Local);

        entry.Update(timestampUtc: localTimestamp);

        Assert.Equal(localTimestamp.ToUniversalTime(), entry.Timestamp);
        Assert.NotNull(entry.ModifiedOnUtc);
    }
}
