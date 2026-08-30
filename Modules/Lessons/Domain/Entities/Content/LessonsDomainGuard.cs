namespace FoodDiary.Domain.Entities.Content;

internal static class LessonsDomainGuard {
    public static void Defined<TEnum>(TEnum value, string paramName) where TEnum : struct, Enum {
        if (!Enum.IsDefined(value)) {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }

    public static DateTime RequiredUtc(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Unspecified
            ? throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.")
            : value.ToUniversalTime();
    }
}
