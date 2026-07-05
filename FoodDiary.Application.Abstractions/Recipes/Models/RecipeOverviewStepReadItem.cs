namespace FoodDiary.Application.Abstractions.Recipes.Models;

public sealed record RecipeOverviewStepReadItem(
    Guid Id,
    int StepNumber,
    string? Title,
    string Instruction,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<RecipeOverviewIngredientReadItem> Ingredients);
