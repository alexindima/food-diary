using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Meals;

public sealed class MealAiItem : Entity<MealAiItemId> {
    private const int NameMaxLength = 256;
    private const int UnitMaxLength = 32;

    public MealAiSessionId MealAiSessionId { get; private set; }
    public string NameEn { get; private set; } = string.Empty;
    public string? NameLocal { get; private set; }
    public double Amount { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public double Calories { get; private set; }
    public double Proteins { get; private set; }
    public double Fats { get; private set; }
    public double Carbs { get; private set; }
    public double Fiber { get; private set; }
    public double Alcohol { get; private set; }

    public MealAiSession Session { get; private set; } = null!;

    private MealAiItem() { }

    internal static MealAiItem Create(
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
        var item = new MealAiItem {
            Id = MealAiItemId.New(),
            NameEn = NormalizeRequiredText(nameEn, NameMaxLength, nameof(nameEn)),
            NameLocal = NormalizeOptionalText(nameLocal, NameMaxLength, nameof(nameLocal)),
            Amount = RequirePositiveFinite(amount, nameof(amount)),
            Unit = NormalizeRequiredText(unit, UnitMaxLength, nameof(unit)),
            Calories = RequireNonNegativeFinite(calories, nameof(calories)),
            Proteins = RequireNonNegativeFinite(proteins, nameof(proteins)),
            Fats = RequireNonNegativeFinite(fats, nameof(fats)),
            Carbs = RequireNonNegativeFinite(carbs, nameof(carbs)),
            Fiber = RequireNonNegativeFinite(fiber, nameof(fiber)),
            Alcohol = RequireNonNegativeFinite(alcohol, nameof(alcohol))
        };
        item.SetCreated();
        return item;
    }

    internal void AttachToSession(MealAiSessionId sessionId) {
        if (sessionId == MealAiSessionId.Empty) {
            throw new ArgumentException("SessionId is required.", nameof(sessionId));
        }

        if (MealAiSessionId == sessionId) {
            return;
        }

        MealAiSessionId = sessionId;
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
