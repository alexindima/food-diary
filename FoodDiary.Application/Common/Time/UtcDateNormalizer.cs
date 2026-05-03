namespace FoodDiary.Application.Common.Time;

public static class UtcDateNormalizer {
    public static DateTime NormalizeDatePreservingUnspecifiedAsUtc(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    public static DateTime NormalizeInstantPreservingUnspecifiedAsUtc(DateTime value) =>
        value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    public static DateTime NormalizeDateUsingLocalFallback(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    public static DateTime NormalizeDateEndUsingLocalFallback(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
    }
}
