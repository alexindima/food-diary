using FoodDiary.Modules.Statistics.Application.Common;

namespace FoodDiary.Modules.Statistics.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class StatisticsPeriodGuardTests {
    [Fact]
    public void ReversedBodyDays_AreRejectedBeforeFallbackInstantsCanReplaceThem() {
        var from = new DateOnly(2026, 10, 21);
        var to = new DateOnly(2026, 10, 20);

        ArgumentException error = Assert.Throws<ArgumentException>(() => StatisticsBodyPeriod.Resolve(from, to, DateTime.UnixEpoch, DateTime.UnixEpoch));

        Assert.Equal("from", error.ParamName);
    }

    [Theory]
    [InlineData("reversed")]
    [InlineData("zone")]
    [InlineData("limit")]
    public void InvalidInstantPeriods_AreRejectedAtTheValueBoundary(string failure) {
        var from = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        DateTime to = failure switch {
            "reversed" => from.AddTicks(-1),
            "limit" => from.AddYears(2),
            _ => from,
        };

        ArgumentException error = Assert.Throws<ArgumentException>(() =>
            StatisticsInstantPeriod.FromRequest(from, to, string.Equals(failure, "zone", StringComparison.Ordinal) ? "FoodDiary/Unknown" : null));

        Assert.Equal("from", error.ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CalendarPeriodPolicy_RejectsReversedOrUnknownZoneRequests(bool reversed) {
        var from = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        bool valid = StatisticsCalendarRangePolicy.IsPeriodWithinLimit(
            from, reversed ? from.AddTicks(-1) : from, reversed ? "Etc/UTC" : "FoodDiary/Unknown");

        Assert.False(valid);
    }
}
