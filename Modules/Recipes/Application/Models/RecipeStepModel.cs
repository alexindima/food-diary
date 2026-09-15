namespace FoodDiary.Modules.Recipes.Application.Models;

public sealed record RecipeStepModel(
    Guid Id,
    int StepNumber,
    string? Title,
    string Instruction,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<RecipeIngredientModel> Ingredients);
