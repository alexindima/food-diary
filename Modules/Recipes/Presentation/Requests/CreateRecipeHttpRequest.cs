namespace FoodDiary.Modules.Recipes.Presentation.Requests;

public sealed record CreateRecipeHttpRequest(
    string Name,
    string? Description,
    string? Comment,
    string? Category,
    string? ImageUrl,
    Guid? ImageAssetId,
    int? PrepTime,
    int? CookTime,
    int Servings,
    string Visibility,
    bool CalculateNutritionAutomatically,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol,
    IReadOnlyList<RecipeStepHttpRequest> Steps) {
    public bool LanguageConfirmed { get; init; }
    public string? Language { get; init; }
    public IReadOnlyList<Guid>? ImageAssetIds { get; init; }
}
