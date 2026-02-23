namespace FoodDiary.Contracts.Recipes;

public record RecipeStepRequest(
    string? Title,
    string Description,
    IReadOnlyList<RecipeIngredientRequest> Ingredients,
    string? ImageUrl,
    Guid? ImageAssetId);
