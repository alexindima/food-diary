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
    double Alcohol) {
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
        double alcohol) {
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
            RequireNonNegativeFinite(alcohol, nameof(alcohol)));
    }

    private static string NormalizeRequiredText(string value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value is required.", paramName);
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
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
}
