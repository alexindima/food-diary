using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class TypedMeasurementDayTests {
    [Fact]
    public void TypedMutation_ChangesOnlyTheDayAndPreservesIdentityAndNoOpAuditBehavior() {
        var userId = UserId.New();
        var originalDay = new MeasurementDay(new DateOnly(2024, 2, 28));
        var nextDay = new MeasurementDay(new DateOnly(2024, 2, 29));
        var weight = WeightEntry.CreateForDay(userId, originalDay, 72.125);
        var waist = WaistEntry.CreateForDay(userId, originalDay, 85.125);
        WeightEntryId weightId = weight.Id;
        WaistEntryId waistId = waist.Id;

        weight.UpdateDetails(day: nextDay);
        waist.UpdateDetails(day: nextDay);
        DateTime? weightStamp = weight.ModifiedOnUtc;
        DateTime? waistStamp = waist.ModifiedOnUtc;
        weight.UpdateDetails(day: nextDay);
        waist.UpdateDetails(day: nextDay);

        Assert.Multiple(() => {
            Assert.Equal(weightId, weight.Id);
            Assert.Equal(waistId, waist.Id);
            Assert.Equal(72.125, weight.WeightKg);
            Assert.Equal(85.125, waist.CircumferenceCm);
            Assert.Equal(nextDay.ToUtcDateTime(), weight.Date);
            Assert.Equal(nextDay.ToUtcDateTime(), waist.Date);
            Assert.Equal(DateTimeKind.Utc, weight.Date.Kind);
            Assert.Equal(DateTimeKind.Utc, waist.Date.Kind);
            Assert.NotNull(weightStamp);
            Assert.NotNull(waistStamp);
            Assert.Equal(weightStamp, weight.ModifiedOnUtc);
            Assert.Equal(waistStamp, waist.ModifiedOnUtc);
        });
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidNumericUpdate_DoesNotChangeTheMeasurementDay(double value) {
        var day = new MeasurementDay(new DateOnly(2024, 2, 29));
        var otherDay = new MeasurementDay(new DateOnly(2024, 3, 1));
        var weight = WeightEntry.CreateForDay(UserId.New(), day, 72);
        var waist = WaistEntry.CreateForDay(UserId.New(), day, 85);

        Assert.Throws<ArgumentOutOfRangeException>(() => weight.UpdateDetails(value, otherDay));
        Assert.Throws<ArgumentOutOfRangeException>(() => waist.UpdateDetails(value, otherDay));

        Assert.Multiple(() => {
            Assert.Equal(day.ToUtcDateTime(), weight.Date);
            Assert.Equal(day.ToUtcDateTime(), waist.Date);
            Assert.Equal(72, weight.WeightKg);
            Assert.Equal(85, waist.CircumferenceCm);
            Assert.Null(weight.ModifiedOnUtc);
            Assert.Null(waist.ModifiedOnUtc);
        });
    }

    [Fact]
    public void TypedCreation_PreservesOwnerValidationAndSeparateNumericLimits() {
        var day = new MeasurementDay(new DateOnly(2024, 2, 29));
        ArgumentException weightOwnerError = Assert.Throws<ArgumentException>(() => WeightEntry.CreateForDay(UserId.Empty, day, double.NaN));
        ArgumentException waistOwnerError = Assert.Throws<ArgumentException>(() => WaistEntry.CreateForDay(UserId.Empty, day, double.NaN));
        Assert.Equal("userId", weightOwnerError.ParamName);
        Assert.Equal("userId", waistOwnerError.ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => WeightEntry.CreateForDay(UserId.New(), day, 500.0001));
        Assert.Throws<ArgumentOutOfRangeException>(() => WaistEntry.CreateForDay(UserId.New(), day, 300.0001));
    }

    [Fact]
    public void OptionalTypedDay_NumericOnlyAndEmptyUpdatesPreserveTheDate() {
        var day = new MeasurementDay(new DateOnly(2025, 12, 31));
        var weight = WeightEntry.CreateForDay(UserId.New(), day, 72);
        var waist = WaistEntry.CreateForDay(UserId.New(), day, 85);
        weight.UpdateDetails();
        waist.UpdateDetails();
        Assert.Null(weight.ModifiedOnUtc);
        Assert.Null(waist.ModifiedOnUtc);

        weight.UpdateDetails(weight: 73.25);
        waist.UpdateDetails(circumference: 86.25);

        Assert.Multiple(() => {
            Assert.Equal(day.ToUtcDateTime(), weight.Date);
            Assert.Equal(day.ToUtcDateTime(), waist.Date);
            Assert.Equal(73.25, weight.WeightKg);
            Assert.Equal(86.25, waist.CircumferenceCm);
        });
    }
}
