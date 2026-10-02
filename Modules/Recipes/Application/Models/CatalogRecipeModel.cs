using FoodDiary.Modules.Recipes.Application.Common;

namespace FoodDiary.Modules.Recipes.Application.Models;

public sealed record CatalogRecipeModel(
    Guid Id,
    string Name,
    string? Description,
    string? Category,
    string? ImageUrl,
    int? PrepTime,
    int? CookTime,
    int Servings,
    string Language,
    bool LanguageConfirmed,
    bool CalculateNutritionAutomatically,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol,
    IReadOnlyList<RecipeStepInput> Steps);
