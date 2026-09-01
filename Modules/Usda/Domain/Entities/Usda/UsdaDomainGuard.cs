using System.Globalization;

namespace FoodDiary.Domain.Entities.Usda;

internal static class UsdaDomainGuard {
    public static double NonNegativeFinite(double value, string paramName) {
        Finite(value, paramName);
        return value < 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.")
            : value;
    }

    public static double PositiveFinite(double value, string paramName) {
        Finite(value, paramName);
        return value <= 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.")
            : value;
    }

    public static int Positive(int value, string paramName) {
        return value <= 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.")
            : value;
    }

    public static int? Positive(int? value, string paramName) {
        return value.HasValue ? Positive(value.Value, paramName) : null;
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

    private static double Finite(double value, string paramName) {
        return double.IsFinite(value)
            ? value
            : throw new ArgumentOutOfRangeException(paramName, "Value must be a finite number.");
    }
}
