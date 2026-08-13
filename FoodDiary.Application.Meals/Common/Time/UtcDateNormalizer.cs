namespace FoodDiary.Application.Meals.Common.Time;

internal static class UtcDateNormalizer {
    public static DateTime NormalizeInstantPreservingUnspecifiedAsUtc(DateTime value) =>
        value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    public static DateTime NormalizeDateUsingLocalFallback(DateTime value) {
        DateTime utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    public static DateTime NormalizeDateEndUsingLocalFallback(DateTime value) {
        DateTime utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        return DateTime.SpecifyKind(utc.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
    }
}
