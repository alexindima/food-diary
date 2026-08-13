namespace FoodDiary.Application.Export.Internal;

internal static class UtcDateNormalizer {
    public static DateTime NormalizeInstantPreservingUnspecifiedAsUtc(DateTime value) =>
        value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
}
