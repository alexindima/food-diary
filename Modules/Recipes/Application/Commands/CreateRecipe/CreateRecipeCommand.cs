using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Application.Models;

namespace FoodDiary.Modules.Recipes.Application.Commands.CreateRecipe;

public record CreateRecipeCommand(
    Guid? UserId,
    string Name,
    string? Description,
    string? Comment,
    string? Category,
    string? ImageUrl,
    Guid? ImageAssetId,
    int? PrepTime,
    int? CookTime,
    int Servings,
    string Visibility,
    bool CalculateNutritionAutomatically,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol,
    IReadOnlyList<RecipeStepInput> Steps) : ICommand<Result<RecipeModel>>, IUserRequest;
