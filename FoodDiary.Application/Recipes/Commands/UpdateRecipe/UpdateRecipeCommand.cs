using System.Collections.Generic;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Recipes.Commands.Common;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

public record UpdateRecipeCommand(
    UserId? UserId,
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
    IReadOnlyList<RecipeStepInput>? Steps) : ICommand<Result<RecipeResponse>>;
