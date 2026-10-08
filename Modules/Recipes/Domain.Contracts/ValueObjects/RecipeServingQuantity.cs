using System.Globalization;

namespace FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;

public sealed record RecipeServingQuantity {
    public const double MaxValue = 1_000_000d;

    private RecipeServingQuantity(double value) => Value = value;

    public double Value { get; }

    public static RecipeServingQuantity FromServings(double servings) {
        if (!double.IsFinite(servings)) {
            throw new ArgumentOutOfRangeException(nameof(servings), "Amount must be a finite number.");
        }

        return servings is <= 0 or > MaxValue
            ? throw new ArgumentOutOfRangeException(nameof(servings), string.Create(CultureInfo.InvariantCulture, $"Amount must be in range (0, {MaxValue}]."))
            : new RecipeServingQuantity(servings);
    }
}
