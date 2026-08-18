using System.Globalization;

namespace FoodDiary.Domain.Common;

internal static class DomainGuard {
    public static TEnum Defined<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum {
        return Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(paramName, "Value must be one of the supported values.");
    }

    public static double Finite(double value, string paramName) {
        return double.IsFinite(value)
            ? value
            : throw new ArgumentOutOfRangeException(paramName, "Value must be a finite number.");
    }

    public static double NonNegativeFinite(double value, string paramName) {
        Finite(value, paramName);
        return value < 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.")
            : value;
    }

    public static double? NonNegativeFinite(double? value, string paramName) {
        return value.HasValue ? NonNegativeFinite(value.Value, paramName) : null;
    }

    public static double PositiveFinite(double value, string paramName) {
        Finite(value, paramName);
        return value <= 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.")
            : value;
    }

    public static double? PositiveFinite(double? value, string paramName) {
        return value.HasValue ? PositiveFinite(value.Value, paramName) : null;
    }

    public static string RequiredText(string value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value is required.", paramName);
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(
                paramName,
                string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."))
            : normalized;
    }

    public static string? OptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(
                paramName,
                string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."))
            : normalized;
    }

    public static DateTime RequiredUtc(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Unspecified
            ? throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.")
            : value.ToUniversalTime();
    }

    public static DateTime? OptionalUtc(DateTime? value, string paramName) {
        return value.HasValue ? RequiredUtc(value.Value, paramName) : null;
    }
}
