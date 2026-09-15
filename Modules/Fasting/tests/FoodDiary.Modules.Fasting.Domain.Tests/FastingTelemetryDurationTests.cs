using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Modules.Fasting.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingTelemetryDurationTests {
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.1)]
    public void FastingTelemetryEvent_Create_WithInvalidActualDuration_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingTelemetryEvent.Create("fasting.completed", Now, actualDurationHours: value));
    }
}
