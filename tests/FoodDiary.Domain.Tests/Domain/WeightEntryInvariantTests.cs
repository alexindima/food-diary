using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public class WeightEntryInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() => WeightEntry.Create(UserId.Empty, DateTime.UtcNow, 70));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(500.0001d)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void Create_WithInvalidWeight_Throws(double weight) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeightEntry.Create(UserId.New(), DateTime.UtcNow, weight));
    }

    [Fact]
    public void Create_WithLocalDate_NormalizesToUtcDate() {
        var localDate = new DateTime(2026, 2, 24, 12, 30, 0, DateTimeKind.Local);
        var expectedUtcDate = DateTime.SpecifyKind(localDate.ToUniversalTime().Date, DateTimeKind.Utc);

        var entry = WeightEntry.Create(UserId.New(), localDate, 72);

        Assert.Equal(expectedUtcDate, entry.Date);
    }

    [Fact]
    public void Create_WithUnspecifiedDate_TreatsItAsUtcDateOnly() {
        var entry = WeightEntry.Create(UserId.New(), new DateTime(2026, 3, 27, 18, 45, 0, DateTimeKind.Unspecified), 72);

        Assert.Equal(new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), entry.Date);
    }

    [Fact]
    public void Update_WithSameValues_DoesNotSetModifiedOnUtc() {
        DateTime date = DateTime.UtcNow.Date;
        var entry = WeightEntry.Create(UserId.New(), date, 72);

        entry.Update(weight: 72, date: date);

        Assert.Null(entry.ModifiedOnUtc);
    }

    [Fact]
    public void Update_WithDifferentValues_SetsModifiedOnUtc() {
        var entry = WeightEntry.Create(UserId.New(), DateTime.UtcNow.Date, 72);
        DateTime newDate = DateTime.UtcNow.AddDays(-1);

        entry.Update(weight: 73.5, date: newDate);

        Assert.Equal(73.5, entry.Weight);
        Assert.Equal(DateTime.SpecifyKind(newDate.ToUniversalTime().Date, DateTimeKind.Utc), entry.Date);
        Assert.NotNull(entry.ModifiedOnUtc);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(500.0001d)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void Update_WithInvalidWeight_Throws(double weight) {
        var entry = WeightEntry.Create(UserId.New(), DateTime.UtcNow, 72);

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.Update(weight: weight));
    }
}
