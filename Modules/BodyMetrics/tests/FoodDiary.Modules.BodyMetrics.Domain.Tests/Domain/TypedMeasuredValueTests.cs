using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class TypedMeasuredValueTests {
    [Fact]
    public void TypedMeasurements_PreserveDayPrecisionAndNoOpAudit() {
        var day = MeasurementDay.FromDateTimeEncoding(new DateTime(2026, 10, 8, 19, 12, 0, DateTimeKind.Unspecified));
        var entry = WeightEntry.CreateWithMeasurement(UserId.New(), day, MeasuredWeightKg.Create(78.123456));
        DateTime? modified = entry.ModifiedOnUtc;
        entry.UpdateMeasurement(MeasuredWeightKg.Create(78.123456), day);
        Assert.Multiple(
            () => Assert.Equal(new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc), entry.Date),
            () => Assert.Equal(78.123456, entry.WeightKg),
            () => Assert.Equal(modified, entry.ModifiedOnUtc));
        entry.UpdateMeasurement(MeasuredWeightKg.Create(77.8));
        Assert.Equal(77.8, entry.WeightKg);
    }

    [Fact]
    public void MeasurementBounds_MatchBothOwners() {
        Assert.Equal(500, MeasuredWeightKg.Create(500).Value);
        Assert.Equal(300, MeasuredWaistCm.Create(300).Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => MeasuredWaistCm.Create(300.01));
        Assert.Throws<ArgumentException>(() => WeightEntry.CreateWithMeasurement(UserId.Empty, default, MeasuredWeightKg.Create(80)));
    }

}
