using System;
using System.Collections.Generic;

namespace FoodDiary.Contracts.Consumptions;

public record ConsumptionResponse(
    int Id,
    DateTime Date,
    string? MealType,
    string? Comment,
    string? ImageUrl,
    double TotalCalories,
    double TotalProteins,
    double TotalFats,
    double TotalCarbs,
    double TotalFiber,
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    IReadOnlyList<ConsumptionItemResponse> Items);

public record ConsumptionItemResponse(
    int Id,
    int ConsumptionId,
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
    Guid? RecipeId,
    string? RecipeName,
    int? RecipeServings,
    double? RecipeTotalCalories,
    double? RecipeTotalProteins,
    double? RecipeTotalFats,
    double? RecipeTotalCarbs,
    double? RecipeTotalFiber);
