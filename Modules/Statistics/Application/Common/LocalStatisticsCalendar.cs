namespace FoodDiary.Application.Statistics.Common;

internal static class LocalStatisticsCalendar {
    internal static IReadOnlyList<LocalStatisticsDay> GetDays(DateTime utcNow, string timeZoneId, int days) {
        if (utcNow.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("A UTC instant is required.", nameof(utcNow));
        }
        if (days is not (1 or 7)) {
            throw new ArgumentOutOfRangeException(nameof(days), "Only today or seven calendar days are supported.");
        }
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone));
        DateOnly first = today.AddDays(1 - days);
        var result = new List<LocalStatisticsDay>(days);
        for (int index = 0; index < days; index++) {
            DateOnly date = first.AddDays(index);
            result.Add(new LocalStatisticsDay(date, StartOfDayUtc(date, zone), StartOfDayUtc(date.AddDays(1), zone)));
        }
        return result;
    }

    private static DateTime StartOfDayUtc(DateOnly date, TimeZoneInfo zone) {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        // Some zones move their clocks at midnight or skip a whole civil date.
        int minutes = 0;
        while (zone.IsInvalidTime(local)) {
            if (++minutes > 2880) {
                throw new InvalidTimeZoneException("Unable to locate a valid calendar day boundary.");
            }
            local = local.AddMinutes(1);
        }
        if (zone.IsAmbiguousTime(local)) {
            // The first midnight is the boundary when a local clock repeats.
            TimeSpan offset = zone.GetAmbiguousTimeOffsets(local).Max();
            return new DateTimeOffset(local, offset).UtcDateTime;
        }
        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }
}
