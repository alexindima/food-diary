namespace FoodDiary.Modules.Recipes.Domain.Nutrition;

/// <summary>Scalar nutrition sources; a valid product source takes precedence over a nested recipe.</summary>
public sealed record RecipeNutritionIngredient(
    double Amount,
    double? ProductBaseAmount,
    RecipeNutritionValues? Product,
    double? NestedRecipeServings,
    RecipeNutritionValues? NestedRecipe);
