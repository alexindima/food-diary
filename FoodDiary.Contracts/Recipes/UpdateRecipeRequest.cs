using System.Collections.Generic;

namespace FoodDiary.Contracts.Recipes;

public record UpdateRecipeRequest(
    string? Name,
    string? Description,
    string? Category,
    string? ImageUrl,
    int? PrepTime,
    int? CookTime,
    int? Servings,
    string? Visibility,
    bool CalculateNutritionAutomatically,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    IReadOnlyList<RecipeStepRequest>? Steps);
