using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

public record UpdateRecipeCommand(
    Guid? UserId,
    RecipeId RecipeId,
    string? Name,
    string? Description,
    string? Comment,
    string? Category,
    string? ImageUrl,
    Guid? ImageAssetId,
    int? PrepTime,
    int? CookTime,
    int? Servings,
    string? Visibility,
    bool CalculateNutritionAutomatically,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol,
    IReadOnlyList<RecipeStepInput>? Steps) : ICommand<Result<RecipeModel>>;
