using FoodDiary.Application.Export.Internal;

namespace FoodDiary.Application.Export.Queries.ExportDiary;

internal static class ExportDiaryDateRangePolicy {
    public static bool TryResolve(
        DateTime dateFrom,
        DateTime dateTo,
        int? timeZoneOffsetMinutes,
        out DateTime normalizedFrom,
        out DateTime normalizedTo,
        out TimeSpan displayOffset) {
        normalizedFrom = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(dateFrom);
        normalizedTo = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(dateTo);

        displayOffset = ResolveDisplayOffset(normalizedFrom, timeZoneOffsetMinutes);
        return CanApplyOffset(normalizedFrom, displayOffset) && CanApplyOffset(normalizedTo, displayOffset);
    }

    private static TimeSpan ResolveDisplayOffset(DateTime dateFrom, int? timeZoneOffsetMinutes) {
        if (timeZoneOffsetMinutes is >= -840 and <= 840) {
            return TimeSpan.FromMinutes(timeZoneOffsetMinutes.Value);
        }

        TimeSpan timeOfDay = dateFrom.TimeOfDay;
        return timeOfDay <= TimeSpan.FromHours(12)
            ? -timeOfDay
            : TimeSpan.FromDays(1) - timeOfDay;
    }

    private static bool CanApplyOffset(DateTime value, TimeSpan offset) =>
        offset.Ticks >= 0
            ? value.Ticks <= DateTime.MaxValue.Ticks - offset.Ticks
            : value.Ticks >= DateTime.MinValue.Ticks - offset.Ticks;
}
