namespace FoodDiary.Modules.Ai.Presentation.Responses;

public sealed record RecipeImportHttpResponse(
    string Name, string? Description, IReadOnlyList<RecipeImportIngredientHttpResponse> Ingredients,
    IReadOnlyList<string> Steps, int? Servings, int? PrepMinutes, int? CookMinutes,
    string? AuthorNutrition, string? SourceUrl);
