namespace FoodDiary.Modules.Recipes.Presentation.Responses;

public sealed record CatalogRecipeImportHttpResponse(
    Guid Id,
    string Status,
    IReadOnlyList<string> Errors);
