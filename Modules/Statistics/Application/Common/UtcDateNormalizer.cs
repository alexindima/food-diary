namespace FoodDiary.Application.Statistics.Common;

internal static class UtcDateNormalizer {
    public static DateTime NormalizeDatePreservingUnspecifiedAsUtc(DateTime value) {
        DateTime utc = NormalizeInstantPreservingUnspecifiedAsUtc(value);
        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    public static DateTime NormalizeInstantPreservingUnspecifiedAsUtc(DateTime value) =>
        value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
}
