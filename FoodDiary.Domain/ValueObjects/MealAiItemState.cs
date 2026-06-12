using System.Globalization;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct MealAiItemState(
    string NameEn,
    string? NameLocal,
    double Amount,
    string Unit,
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol,
    double Confidence,
    MealAiItemResolution Resolution) {
    private const int NameMaxLength = 256;
    private const int UnitMaxLength = 32;

    public static MealAiItemState Create(
        string nameEn,
        string? nameLocal,
        double amount,
        string unit,
        double calories,
        double proteins,
        double fats,
        double carbs,
        double fiber,
        double alcohol,
        double confidence = 1,
        MealAiItemResolution resolution = MealAiItemResolution.Accepted) {
        return new MealAiItemState(
            NormalizeRequiredText(nameEn, NameMaxLength, nameof(nameEn)),
            NormalizeOptionalText(nameLocal, NameMaxLength, nameof(nameLocal)),
            RequirePositiveFinite(amount, nameof(amount)),
            NormalizeRequiredText(unit, UnitMaxLength, nameof(unit)),
            RequireNonNegativeFinite(calories, nameof(calories)),
            RequireNonNegativeFinite(proteins, nameof(proteins)),
            RequireNonNegativeFinite(fats, nameof(fats)),
            RequireNonNegativeFinite(carbs, nameof(carbs)),
            RequireNonNegativeFinite(fiber, nameof(fiber)),
            RequireNonNegativeFinite(alcohol, nameof(alcohol)),
            RequireConfidence(confidence, nameof(confidence)),
            NormalizeResolution(resolution));
    }

    private static string NormalizeRequiredText(string value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value is required.", paramName);
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."))
            : normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."))
            : normalized;
    }

    private static double RequirePositiveFinite(double value, string paramName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be a finite number.");
        }

        return value <= 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.")
            : value;
    }

    private static double RequireNonNegativeFinite(double value, string paramName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be a finite number.");
        }

        return value < 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.")
            : value;
    }

    private static double RequireConfidence(double value, string paramName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(paramName, "Confidence must be a finite number.");
        }

        return value is < 0 or > 1
            ? throw new ArgumentOutOfRangeException(paramName, "Confidence must be in range [0, 1].")
            : value;
    }

    private static MealAiItemResolution NormalizeResolution(MealAiItemResolution resolution) {
        return Enum.IsDefined(resolution)
            ? resolution
            : throw new ArgumentOutOfRangeException(nameof(resolution), "Unknown AI item resolution value.");
    }
}
