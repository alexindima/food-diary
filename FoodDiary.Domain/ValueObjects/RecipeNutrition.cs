namespace FoodDiary.Domain.ValueObjects;

public readonly record struct RecipeNutrition(
    double? Calories,
    double? Proteins,
    double? Fats,
    double? Carbs,
    double? Fiber,
    double? Alcohol) {
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
        if (value is < 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.");
        }

        return value;
    }
}
