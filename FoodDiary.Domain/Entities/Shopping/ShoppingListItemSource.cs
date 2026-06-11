using System.Globalization;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Shopping;

public sealed class ShoppingListItemSource : Entity<ShoppingListItemSourceId> {
    private const int LabelMaxLength = 256;

    public ShoppingListItemId ShoppingListItemId { get; private set; }
    public ShoppingListItemSourceType SourceType { get; private set; }
    public MealPlanId? MealPlanId { get; private set; }
    public MealPlanMealId? MealPlanMealId { get; private set; }
    public RecipeId? RecipeId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public int? DayNumber { get; private set; }
    public string? MealType { get; private set; }
    public double Amount { get; private set; }
    public MeasurementUnit? Unit { get; private set; }

    public ShoppingListItem ShoppingListItem { get; private set; } = null!;

    private ShoppingListItemSource() {
    }

    public static ShoppingListItemSource CreateMealPlanSource(
        ShoppingListItemId shoppingListItemId,
        MealPlanId mealPlanId,
        MealPlanMealId mealPlanMealId,
        RecipeId recipeId,
        string label,
        int dayNumber,
        string mealType,
        double amount,
        MeasurementUnit? unit) {
        EnsureShoppingListItemId(shoppingListItemId);
        EnsureMealPlanId(mealPlanId);
        EnsureMealPlanMealId(mealPlanMealId);
        EnsureRecipeId(recipeId);
        string normalizedLabel = NormalizeRequiredLabel(label);
        double normalizedAmount = ValidateAmount(amount, nameof(amount));

        if (dayNumber <= 0) {
            throw new ArgumentOutOfRangeException(nameof(dayNumber), "Day number must be positive.");
        }

        var source = new ShoppingListItemSource {
            Id = ShoppingListItemSourceId.New(),
            ShoppingListItemId = shoppingListItemId,
            SourceType = ShoppingListItemSourceType.MealPlan,
            MealPlanId = mealPlanId,
            MealPlanMealId = mealPlanMealId,
            RecipeId = recipeId,
            Label = normalizedLabel,
            DayNumber = dayNumber,
            MealType = NormalizeOptionalText(mealType, LabelMaxLength, nameof(mealType)),
            Amount = normalizedAmount,
            Unit = unit,
        };
        source.SetCreated();
        return source;
    }

    private static string NormalizeRequiredLabel(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Source label is required.", nameof(value));
        }

        string normalized = value.Trim();
        return normalized.Length > LabelMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Source label must be at most {LabelMaxLength} characters.")
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

    private static double ValidateAmount(double amount, string paramName) {
        if (double.IsNaN(amount) || double.IsInfinity(amount)) {
            throw new ArgumentOutOfRangeException(paramName, "Amount must be a finite number.");
        }

        return amount <= 0
            ? throw new ArgumentOutOfRangeException(paramName, "Amount must be greater than zero.")
            : amount;
    }

    private static void EnsureShoppingListItemId(ShoppingListItemId shoppingListItemId) {
        if (shoppingListItemId == ShoppingListItemId.Empty) {
            throw new ArgumentException("ShoppingListItemId is required.", nameof(shoppingListItemId));
        }
    }

    private static void EnsureMealPlanId(global::FoodDiary.Domain.ValueObjects.Ids.MealPlanId mealPlanId) {
        if (mealPlanId == global::FoodDiary.Domain.ValueObjects.Ids.MealPlanId.Empty) {
            throw new ArgumentException("MealPlanId is required.", nameof(mealPlanId));
        }
    }

    private static void EnsureMealPlanMealId(global::FoodDiary.Domain.ValueObjects.Ids.MealPlanMealId mealPlanMealId) {
        if (mealPlanMealId == global::FoodDiary.Domain.ValueObjects.Ids.MealPlanMealId.Empty) {
            throw new ArgumentException("MealPlanMealId is required.", nameof(mealPlanMealId));
        }
    }

    private static void EnsureRecipeId(global::FoodDiary.Domain.ValueObjects.Ids.RecipeId recipeId) {
        if (recipeId == global::FoodDiary.Domain.ValueObjects.Ids.RecipeId.Empty) {
            throw new ArgumentException("RecipeId is required.", nameof(recipeId));
        }
    }
}
