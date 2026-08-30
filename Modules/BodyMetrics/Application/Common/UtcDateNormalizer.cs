namespace FoodDiary.Application.BodyMetrics.Common;

internal static class UtcDateNormalizer {
    public static DateTime NormalizeDatePreservingUnspecifiedAsUtc(DateTime value) {
        DateTime utc = value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
