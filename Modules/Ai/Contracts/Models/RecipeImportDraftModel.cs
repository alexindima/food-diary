namespace FoodDiary.Modules.Ai.Contracts.Models;

public sealed record RecipeImportDraftModel(
    string Name,
    string? Description,
    IReadOnlyList<RecipeImportIngredientModel> Ingredients,
    IReadOnlyList<string> Steps,
    int? Servings,
    int? PrepMinutes,
    int? CookMinutes,
    string? AuthorNutrition,
    string? SourceUrl);
