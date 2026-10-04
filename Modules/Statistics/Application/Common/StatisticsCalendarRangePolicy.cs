using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Statistics.Application.Common;

internal static class StatisticsCalendarRangePolicy {
    public static bool IsTimeZoneValid(string? timeZoneId) =>
        LocalCalendar.TryResolve(timeZoneId, offsetMinutes: null, out _);

    public static bool IsPeriodWithinLimit(DateTime from, DateTime to, string? timeZoneId) {
        if (timeZoneId is null) {
            return TemporalRangePolicy.IsInstantPeriodWithinLimit(from, to);
        }

        if (from > to || !LocalCalendar.TryResolve(timeZoneId, offsetMinutes: null, out TimeZoneInfo zone)) {
            return false;
        }

        DateTime normalizedFrom = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(from);
        DateTime normalizedTo = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(to);
        return LocalCalendar.DateAt(normalizedTo, zone).DayNumber - LocalCalendar.DateAt(normalizedFrom, zone).DayNumber
            < TemporalRangePolicy.MaxPeriodDays;
    }
}
