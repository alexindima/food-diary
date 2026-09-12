using FoodDiary.Application.Statistics.Common;

namespace FoodDiary.Application.Tests.Statistics;

[ExcludeFromCodeCoverage]
public sealed class LocalStatisticsCalendarTests {
    [Theory]
    [InlineData(2026, 3, 8, 23)]
    [InlineData(2026, 11, 1, 25)]
    public void NewYorkDay_UsesBothMidnightOffsets(int year, int month, int day, int hours) {
        DateTime now = new(year, month, day, 12, 0, 0, DateTimeKind.Utc);
        LocalStatisticsDay result = Assert.Single(LocalStatisticsCalendar.GetDays(now, "America/New_York", 1));
        Assert.Equal(new DateOnly(year, month, day), result.Date);
        Assert.Equal(TimeSpan.FromHours(hours), result.EndExclusiveUtc - result.StartUtc);
        Assert.Equal(DateTimeKind.Utc, result.StartUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, result.EndExclusiveUtc.Kind);
    }

    [Fact]
    public void SevenDays_AreCalendarDatesAndHaveAdjacentBoundariesAcrossDst() {
        IReadOnlyList<LocalStatisticsDay> days = LocalStatisticsCalendar.GetDays(
            new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc), "America/New_York", 7);
        Assert.Equal(7, days.Count);
        Assert.Equal(new DateOnly(2026, 3, 4), days[0].Date);
        Assert.Equal(new DateOnly(2026, 3, 10), days[^1].Date);
        Assert.Equal(TimeSpan.FromHours(167), days[^1].EndExclusiveUtc - days[0].StartUtc);
        for (int index = 1; index < days.Count; index++) {
            Assert.Equal(days[index - 1].EndExclusiveUtc, days[index].StartUtc);
        }
    }

    [Fact]
    public void PositiveOffset_SelectsTheUsersDateInsteadOfUtcDate() {
        LocalStatisticsDay day = Assert.Single(LocalStatisticsCalendar.GetDays(
            new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Utc), "Pacific/Kiritimati", 1));
        Assert.Equal(new DateOnly(2026, 9, 12), day.Date);
        Assert.Equal(new DateTime(2026, 9, 11, 10, 0, 0, DateTimeKind.Utc), day.StartUtc);
    }

    [Fact]
    public void MidnightClockJump_StartsAtFirstValidInstant() {
        LocalStatisticsDay day = Assert.Single(LocalStatisticsCalendar.GetDays(
            new DateTime(2018, 11, 4, 12, 0, 0, DateTimeKind.Utc), "America/Sao_Paulo", 1));
        Assert.Equal(new DateTime(2018, 11, 4, 3, 0, 0, DateTimeKind.Utc), day.StartUtc);
        Assert.Equal(TimeSpan.FromHours(23), day.EndExclusiveUtc - day.StartUtc);
    }

    [Fact]
    public void InvalidInputs_AreRejectedInsteadOfSilentlyUsingServerTime() {
        Assert.Throws<TimeZoneNotFoundException>(() => LocalStatisticsCalendar.GetDays(DateTime.UtcNow, "Invalid/Zone", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => LocalStatisticsCalendar.GetDays(DateTime.UtcNow, "UTC", 2));
        Assert.Throws<ArgumentException>(() => LocalStatisticsCalendar.GetDays(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified), "UTC", 1));
    }
}
