using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Recipes.Domain.Nutrition;

public sealed record RecipeNutritionIngredient {
    private RecipeNutritionIngredient(RecipeNutritionSource kind, double scaleFactor, RecipeNutritionValues? nutrition) {
        Kind = kind;
        ScaleFactor = scaleFactor;
        Nutrition = nutrition;
    }

    public static RecipeNutritionIngredient Unavailable { get; } = new(RecipeNutritionSource.None, scaleFactor: 0, nutrition: null);

    public RecipeNutritionSource Kind { get; }
    public double ScaleFactor { get; }
    public RecipeNutritionValues? Nutrition { get; }

    public static RecipeNutritionIngredient FromProduct(ProductUnitQuantity amount, double? baseAmount, RecipeNutritionValues? nutrition) {
        ArgumentNullException.ThrowIfNull(amount);
        return FromStoredProduct(amount.Value, baseAmount, nutrition);
    }

    public static RecipeNutritionIngredient FromRecipe(RecipeServingQuantity servings, double? totalServings, RecipeNutritionValues? nutrition) {
        ArgumentNullException.ThrowIfNull(servings);
        return FromStoredRecipe(servings.Value, totalServings, nutrition);
    }

    public static RecipeNutritionIngredient FromStoredProduct(double amountInProductUnits, double? baseAmount, RecipeNutritionValues? nutrition) =>
        baseAmount is > 0
            ? new RecipeNutritionIngredient(RecipeNutritionSource.Product, amountInProductUnits / baseAmount.Value, nutrition)
            : Unavailable;

    public static RecipeNutritionIngredient FromStoredRecipe(double servingQuantity, double? totalServings, RecipeNutritionValues? nutrition) =>
        totalServings is > 0
            ? new RecipeNutritionIngredient(RecipeNutritionSource.Recipe, servingQuantity / totalServings.Value, nutrition)
            : Unavailable;

    // Stored projections may contain both alternatives or incomplete legacy values.
    // Preserve the existing product-base precedence before exposing one selected source.
    public static RecipeNutritionIngredient FromStoredSources(
        double amount,
        double? productBaseAmount,
        RecipeNutritionValues? product,
        double? nestedRecipeServings,
        RecipeNutritionValues? nestedRecipe) =>
        productBaseAmount is > 0
            ? FromStoredProduct(amount, productBaseAmount, product)
            : FromStoredRecipe(amount, nestedRecipeServings, nestedRecipe);
}
