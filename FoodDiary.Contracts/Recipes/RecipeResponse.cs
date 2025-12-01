using System;
using System.Collections.Generic;

namespace FoodDiary.Contracts.Recipes;

public record RecipeResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Category,
    string? ImageUrl,
    Guid? ImageAssetId,
    int? PrepTime,
    int? CookTime,
    int Servings,
    double? TotalCalories,
    double? TotalProteins,
    double? TotalFats,
    double? TotalCarbs,
    double? TotalFiber,
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    string Visibility,
    int UsageCount,
    DateTime CreatedAt,
    bool IsOwnedByCurrentUser,
    IReadOnlyList<RecipeStepResponse> Steps);

public record RecipeStepResponse(
    Guid Id,
    int StepNumber,
    string Instruction,
    string? ImageUrl,
    IReadOnlyList<RecipeIngredientResponse> Ingredients);

public record RecipeIngredientResponse(
    Guid Id,
    double Amount,
    Guid? ProductId,
    string? ProductName,
    string? ProductBaseUnit,
    double? ProductBaseAmount,
    double? ProductCaloriesPerBase,
    double? ProductProteinsPerBase,
    double? ProductFatsPerBase,
    double? ProductCarbsPerBase,
    double? ProductFiberPerBase,
    Guid? NestedRecipeId,
    string? NestedRecipeName);
