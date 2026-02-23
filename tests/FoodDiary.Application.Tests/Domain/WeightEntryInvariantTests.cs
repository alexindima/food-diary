using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class WeightEntryInvariantTests
{
    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => WeightEntry.Create(UserId.Empty, DateTime.UtcNow, 70));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(500.0001d)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void Create_WithInvalidWeight_Throws(double weight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeightEntry.Create(UserId.New(), DateTime.UtcNow, weight));
    }

    [Fact]
    public void Create_WithLocalDate_NormalizesToUtcDate()
    {
        var localDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

        var entry = WeightEntry.Create(UserId.New(), localDate, 72);

        Assert.Equal(DateTimeKind.Utc, entry.Date.Kind);
        Assert.Equal(localDate.Date, entry.Date.Date);
    }

    [Fact]
    public void Update_WithSameValues_DoesNotSetModifiedOnUtc()
    {
        var date = DateTime.UtcNow.Date;
        var entry = WeightEntry.Create(UserId.New(), date, 72);

        entry.Update(weight: 72, date: date);

        Assert.Null(entry.ModifiedOnUtc);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(500.0001d)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void Update_WithInvalidWeight_Throws(double weight)
    {
        var entry = WeightEntry.Create(UserId.New(), DateTime.UtcNow, 72);

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.Update(weight: weight));
    }
}
