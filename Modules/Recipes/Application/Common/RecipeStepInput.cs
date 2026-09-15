namespace FoodDiary.Modules.Recipes.Application.Common;

public record RecipeStepInput(
    int Order,
    string Description,
    string? Title,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<RecipeIngredientInput> Ingredients);
