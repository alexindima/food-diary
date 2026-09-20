namespace FoodDiary.Application.Abstractions.Common.Validation;

/// <summary>Calendar boundaries are resolved independently so offset changes never imply 24-hour days.</summary>
public static class LocalCalendar {
    public static bool TryResolve(string? timeZoneId, int? offsetMinutes, out TimeZoneInfo zone) {
        zone = TimeZoneInfo.Utc;
        if (offsetMinutes is < -840 or > 840) {
            return false;
        }
        if (timeZoneId is null) {
            zone = offsetMinutes is null or 0 ? TimeZoneInfo.Utc
                : TimeZoneInfo.CreateCustomTimeZone("client-offset", TimeSpan.FromMinutes(offsetMinutes.Value), "client-offset", "client-offset");
            return true;
        }
        if (string.IsNullOrWhiteSpace(timeZoneId) || timeZoneId.Length > 100) {
            return false;
        }
        try {
            zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return zone.HasIanaId || string.Equals(zone.Id, "UTC", StringComparison.Ordinal);
        } catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException) {
            return false;
        }
    }

    public static DateTime StartOfDayUtc(DateOnly date, TimeZoneInfo zone) {
        var local = date.ToDateTime(TimeOnly.MinValue);
        // Some zones advance the clock at midnight (or skip a calendar date entirely).
        while (zone.IsInvalidTime(local)) {
            local = local.AddMinutes(1);
        }
        TimeSpan offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
        return new DateTime(checked(local.Ticks - offset.Ticks), DateTimeKind.Utc);
    }

    public static DateOnly DateAt(DateTime utc, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone));

    public static IReadOnlyList<(DateTime Start, DateTime End)> BuildBuckets(
        DateTime fromUtc, DateTime toUtc, int quantizationDays, TimeZoneInfo zone) {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fromUtc, toUtc);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantizationDays, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantizationDays, TemporalRangePolicy.MaxQuantizationDays);
        var buckets = new List<(DateTime Start, DateTime End)>();
        DateTime start = fromUtc;
        DateOnly last = DateAt(toUtc, zone);
        if (last.DayNumber - DateAt(fromUtc, zone).DayNumber >= TemporalRangePolicy.MaxPeriodDays) {
            throw new ArgumentOutOfRangeException(nameof(toUtc), "Calendar range exceeds the allowed period.");
        }
        while (start <= toUtc) {
            DateOnly date = DateAt(start, zone);
            if (last.DayNumber - date.DayNumber < quantizationDays) {
                buckets.Add((start, toUtc));
                break;
            }
            DateTime next = StartOfDayUtc(date.AddDays(quantizationDays), zone);
            buckets.Add((start, next.AddTicks(-1)));
            start = next;
        }
        return buckets;
    }
}
