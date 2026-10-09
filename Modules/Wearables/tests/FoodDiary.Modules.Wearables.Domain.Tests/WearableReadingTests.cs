using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Wearables.Domain.ValueObjects;

namespace FoodDiary.Modules.Wearables.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class WearableReadingTests {
    [Theory]
    [InlineData(WearableDataType.Steps)]
    [InlineData(WearableDataType.HeartRate)]
    [InlineData(WearableDataType.CaloriesBurned)]
    [InlineData(WearableDataType.ActiveMinutes)]
    [InlineData(WearableDataType.SleepMinutes)]
    public void Reading_PreservesFractionalValuesWithoutNewCaps(WearableDataType type) {
        Assert.Equal(1000000.25, WearableReading.FromFields(type, 1000000.25).Value);
        Assert.Equal(0, WearableReading.FromFields(type, 0).Value);
    }

    [Fact]
    public void UpdateReading_RejectsCrossMetricWithoutMutatingEntry() {
        var entry = WearableSyncEntry.CreateWithReading(UserId.New(), WearableProvider.Fitbit,
            WearableSyncDay.FromDate(new DateTime(2026, 10, 8, 16, 30, 0, DateTimeKind.Unspecified)), WearableReading.Steps(2.5));
        Assert.Throws<ArgumentException>(() => entry.UpdateReading(WearableReading.HeartRate(60)));
        Assert.Equal(2.5, entry.Value);
        Assert.Equal(new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc), entry.Date);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Reading_RejectsInvalidValues(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => WearableReading.Steps(value));
}
