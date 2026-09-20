using FoodDiary.Application.Abstractions.Common.Validation;
using System.Globalization;

namespace FoodDiary.Application.Contracts.Tests.Validation;

[ExcludeFromCodeCoverage]
public sealed class LocalCalendarTests {
    [Fact]
    public void BuildBuckets_WhenCalendarPeriodExceedsLimit_RejectsRange() {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LocalCalendar.BuildBuckets(from, from.AddDays(TemporalRangePolicy.MaxPeriodDays), 1, TimeZoneInfo.Utc));
        Assert.Equal("toUtc", error.ParamName);
    }

    public static IEnumerable<object[]> CalendarCases() {
        string[] zones = ["UTC", "Asia/Tbilisi", "America/Los_Angeles", "America/St_Johns", "Asia/Kathmandu", "Pacific/Kiritimati", "Pacific/Pago_Pago", "Europe/Berlin", "Australia/Lord_Howe"];
        string[] dates = ["2024-02-29", "2026-01-01", "2026-03-08", "2026-03-29", "2026-04-05", "2026-09-20", "2026-10-04", "2026-10-25", "2026-11-01", "2026-12-31"];
        foreach (string zone in zones) {
            foreach (string date in dates) {
                yield return [zone, date];
            }
        }
    }

    [Theory]
    [MemberData(nameof(CalendarCases))]
    public void DayAndWeek_PreserveLocalDatesAndContiguousBoundaries(string zoneId, string dateText) {
        var day = DateOnly.ParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        Assert.True(LocalCalendar.TryResolve(zoneId, offsetMinutes: null, out TimeZoneInfo zone));
        DateTime start = LocalCalendar.StartOfDayUtc(day, zone);
        DateTime end = LocalCalendar.StartOfDayUtc(day.AddDays(1), zone).AddTicks(-1);
        Assert.Equal(TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone), start);
        Assert.Equal(day, LocalCalendar.DateAt(start, zone));
        Assert.Equal(day, LocalCalendar.DateAt(end, zone));
        Assert.Equal(day.AddDays(-1), LocalCalendar.DateAt(start.AddTicks(-1), zone));
        Assert.Equal(day.AddDays(1), LocalCalendar.DateAt(end.AddTicks(1), zone));
        IReadOnlyList<(DateTime Start, DateTime End)> buckets = LocalCalendar.BuildBuckets(LocalCalendar.StartOfDayUtc(day.AddDays(-6), zone), end, 1, zone);
        Assert.Equal(7, buckets.Count);
        for (int index = 0; index < buckets.Count; index++) {
            Assert.Equal(day.AddDays(index - 6), LocalCalendar.DateAt(buckets[index].Start, zone));
            Assert.Equal(day.AddDays(index - 6), LocalCalendar.DateAt(buckets[index].End, zone));
            if (index > 0) {
                Assert.Equal(buckets[index - 1].End.AddTicks(1), buckets[index].Start);
            }
        }
    }

    [Theory]
    [InlineData("Europe/Berlin", "2026-03-29", 23)]
    [InlineData("Europe/Berlin", "2026-10-25", 25)]
    [InlineData("America/Los_Angeles", "2026-03-08", 23)]
    [InlineData("America/Los_Angeles", "2026-11-01", 25)]
    [InlineData("Australia/Lord_Howe", "2026-10-04", 23.5)]
    [InlineData("Australia/Lord_Howe", "2026-04-05", 24.5)]
    [InlineData("America/Sao_Paulo", "2018-11-04", 23)]
    public void DayLength_AccountsForClockChanges(string zoneId, string dateText, double hours) {
        var day = DateOnly.ParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        Assert.Equal(hours, (LocalCalendar.StartOfDayUtc(day.AddDays(1), zone) - LocalCalendar.StartOfDayUtc(day, zone)).TotalHours);
    }

    [Theory]
    [InlineData("unknown/zone", null)]
    [InlineData("", null)]
    [InlineData(" ", null)]
    [InlineData(null, 841)]
    [InlineData(null, -841)]
    public void InvalidZones_AreRejected(string? id, int? offset) => Assert.False(LocalCalendar.TryResolve(id, offset, out _));

    [Fact]
    public void LegacyOffset_IsSupported_AndNamedZoneTakesPrecedence() {
        var day = new DateOnly(2026, 9, 20);
        Assert.True(LocalCalendar.TryResolve(timeZoneId: null, 240, out TimeZoneInfo fixedZone));
        Assert.True(LocalCalendar.TryResolve("Asia/Tbilisi", 0, out TimeZoneInfo namedZone));
        Assert.Equal(new DateTime(2026, 9, 19, 20, 0, 0, DateTimeKind.Utc), LocalCalendar.StartOfDayUtc(day, fixedZone));
        Assert.Equal(LocalCalendar.StartOfDayUtc(day, fixedZone), LocalCalendar.StartOfDayUtc(day, namedZone));
    }
}
