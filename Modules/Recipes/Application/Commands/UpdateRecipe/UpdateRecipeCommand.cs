using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Application.Models;

namespace FoodDiary.Modules.Recipes.Application.Commands.UpdateRecipe;

public record UpdateRecipeCommand(
    Guid? UserId,
    Guid RecipeId,
    string? Name,
    string? Description,
    bool ClearDescription,
    string? Comment,
    bool ClearComment,
    string? Category,
    bool ClearCategory,
    string? ImageUrl,
    bool ClearImageUrl,
    Guid? ImageAssetId,
    bool ClearImageAssetId,
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
    IReadOnlyList<RecipeStepInput>? Steps) : ICommand<Result<RecipeModel>>, IUserRequest;
