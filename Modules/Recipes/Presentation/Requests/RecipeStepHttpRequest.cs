namespace FoodDiary.Modules.Recipes.Presentation.Requests;

public sealed record RecipeStepHttpRequest(
    string? Title,
    string Description,
    IReadOnlyList<RecipeIngredientHttpRequest> Ingredients,
    string? ImageUrl,
    Guid? ImageAssetId);
