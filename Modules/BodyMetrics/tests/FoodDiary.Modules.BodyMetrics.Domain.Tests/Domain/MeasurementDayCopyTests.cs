using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects;

namespace FoodDiary.Modules.BodyMetrics.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class MeasurementDayCopyTests {
    [Fact]
    public void Copy_ReplacesTheCalendarDayAndKeepsBothUtcEncodingsIndependent() {
        var original = new MeasurementDay(new DateOnly(2024, 2, 29));

        MeasurementDay copied = original with { Value = new DateOnly(2024, 3, 1) };

        Assert.Multiple(
            () => Assert.Equal(new DateOnly(2024, 2, 29), original.Value),
            () => Assert.Equal(new DateOnly(2024, 3, 1), copied.Value),
            () => Assert.Equal(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc), original.ToUtcDateTime()),
            () => Assert.Equal(new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc), copied.ToUtcDateTime()),
            () => Assert.Equal(DateTimeKind.Utc, original.ToUtcDateTime().Kind),
            () => Assert.Equal(DateTimeKind.Utc, copied.ToUtcDateTime().Kind));
    }
}
