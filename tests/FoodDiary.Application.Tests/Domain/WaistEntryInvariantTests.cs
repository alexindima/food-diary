using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class WaistEntryInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() => WaistEntry.Create(UserId.Empty, DateTime.UtcNow, 80));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(300.0001d)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void Create_WithInvalidCircumference_Throws(double circumference) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WaistEntry.Create(UserId.New(), DateTime.UtcNow, circumference));
    }

    [Fact]
    public void Create_WithLocalDate_NormalizesToUtcDate() {
        var localDate = new DateTime(2026, 2, 24, 12, 30, 0, DateTimeKind.Local);
        var expectedUtcDate = DateTime.SpecifyKind(localDate.ToUniversalTime().Date, DateTimeKind.Utc);

        var entry = WaistEntry.Create(UserId.New(), localDate, 85);

        Assert.Equal(expectedUtcDate, entry.Date);
    }

    [Fact]
    public void Create_WithUnspecifiedDate_TreatsItAsUtcDateOnly() {
        var entry = WaistEntry.Create(UserId.New(), new DateTime(2026, 3, 27, 18, 45, 0, DateTimeKind.Unspecified), 85);

        Assert.Equal(new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), entry.Date);
    }

    [Fact]
    public void Update_WithSameValues_DoesNotSetModifiedOnUtc() {
        var date = DateTime.UtcNow.Date;
        var entry = WaistEntry.Create(UserId.New(), date, 85);

        entry.Update(circumference: 85, date: date);

        Assert.Null(entry.ModifiedOnUtc);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(300.0001d)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void Update_WithInvalidCircumference_Throws(double circumference) {
        var entry = WaistEntry.Create(UserId.New(), DateTime.UtcNow, 85);

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.Update(circumference: circumference));
    }
}
