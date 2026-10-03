namespace FoodDiary.Modules.Recipes.Presentation.Requests;

public sealed record CatalogRecipeHttpRequest(
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
    IReadOnlyList<CatalogRecipeStepHttpRequest> Steps);
