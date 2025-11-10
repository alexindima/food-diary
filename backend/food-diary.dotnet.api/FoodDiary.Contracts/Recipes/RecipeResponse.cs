using System;
using System.Collections.Generic;

namespace FoodDiary.Contracts.Recipes;

public record RecipeResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Category,
    string? ImageUrl,
    int? PrepTime,
    int? CookTime,
    int Servings,
    double? TotalCalories,
    double? TotalProteins,
    double? TotalFats,
    double? TotalCarbs,
    double? TotalFiber,
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
    Guid? NestedRecipeId,
    string? NestedRecipeName);
