using System.Runtime.InteropServices;

namespace FoodDiary.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct RecipeNutrition {
    public double? Calories { get; }
    public double? Proteins { get; }
    public double? Fats { get; }
    public double? Carbs { get; }
    public double? Fiber { get; }
    public double? Alcohol { get; }

    public RecipeNutrition(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber,
        double? alcohol) {
        Calories = EnsureNonNegative(calories, nameof(calories));
        Proteins = EnsureNonNegative(proteins, nameof(proteins));
        Fats = EnsureNonNegative(fats, nameof(fats));
        Carbs = EnsureNonNegative(carbs, nameof(carbs));
        Fiber = EnsureNonNegative(fiber, nameof(fiber));
        Alcohol = EnsureNonNegative(alcohol, nameof(alcohol));
    }

    public static RecipeNutrition Create(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber,
        double? alcohol) {
        return new RecipeNutrition(
            EnsureNonNegative(calories, nameof(calories)),
            EnsureNonNegative(proteins, nameof(proteins)),
            EnsureNonNegative(fats, nameof(fats)),
            EnsureNonNegative(carbs, nameof(carbs)),
            EnsureNonNegative(fiber, nameof(fiber)),
            EnsureNonNegative(alcohol, nameof(alcohol)));
    }

    private static double? EnsureNonNegative(double? value, string paramName) {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value))) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be a finite number.");
        }

        return value is < 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.")
            : value;
    }
}
