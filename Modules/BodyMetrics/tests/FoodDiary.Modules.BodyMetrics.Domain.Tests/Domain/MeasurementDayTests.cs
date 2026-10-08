using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects;

namespace FoodDiary.Modules.BodyMetrics.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class MeasurementDayTests {
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2024, 2, 29)]
    [InlineData(2025, 12, 31)]
    [InlineData(2026, 3, 29)]
    [InlineData(2026, 10, 25)]
    [InlineData(9999, 12, 31)]
    public void CalendarDate_EncodesAsUtcMidnightWithoutAZoneShift(int year, int month, int day) {
        var date = new DateOnly(year, month, day);
        var measurement = new MeasurementDay(date);

        DateTime encoded = measurement.ToUtcDateTime();

        Assert.Multiple(() => {
            Assert.Equal(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc), encoded);
            Assert.Equal(DateTimeKind.Utc, encoded.Kind);
            Assert.Equal(measurement, MeasurementDay.FromDateTimeEncoding(encoded));
        });
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    public void LegacyEncoding_DiscardsClockTimeAndPreservesTheEncodedCalendarDay(DateTimeKind kind) {
        var encoded = new DateTime(2024, 2, 29, 23, 59, 59, kind);

        var measurement = MeasurementDay.FromDateTimeEncoding(encoded);

        Assert.Equal(new DateOnly(2024, 2, 29), measurement.Value);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(23, 59)]
    public void LegacyLocalEncoding_RetainsTheOriginalUtcDayAtMidnightEdges(int hour, int minute) {
        var utc = new DateTime(2024, 2, 29, hour, minute, 0, DateTimeKind.Utc);

        var measurement = MeasurementDay.FromDateTimeEncoding(utc.ToLocalTime());

        Assert.Equal(new DateOnly(2024, 2, 29), measurement.Value);
    }

    [Fact]
    public void Default_IsThePreviouslyRepresentableMinimumCalendarDate() {
        MeasurementDay measurement = default;

        Assert.Equal(new MeasurementDay(DateOnly.MinValue), measurement);
        Assert.Equal(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), measurement.ToUtcDateTime());
    }
}
