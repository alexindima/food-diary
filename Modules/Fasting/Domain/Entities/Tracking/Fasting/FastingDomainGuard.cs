namespace FoodDiary.Domain.Entities.Tracking.Fasting;

internal static class FastingDomainGuard {
    public static TEnum Defined<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum {
        return Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(paramName, "Value must be one of the supported values.");
    }

    public static double? NonNegativeFinite(double? value, string paramName) {
        if (!value.HasValue) {
            return null;
        }

        if (!double.IsFinite(value.Value) || value.Value < 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be a finite non-negative number.");
        }

        return value;
    }

    public static DateTime RequiredUtc(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Unspecified
            ? throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.")
            : value.ToUniversalTime();
    }
}
