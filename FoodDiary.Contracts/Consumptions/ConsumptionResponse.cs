using System;
using System.Collections.Generic;

namespace FoodDiary.Contracts.Consumptions;

public record ConsumptionResponse(
    Guid Id,
    DateTime Date,
    string? MealType,
    string? Comment,
    string? ImageUrl,
    Guid? ImageAssetId,
    double TotalCalories,
    double TotalProteins,
    double TotalFats,
    double TotalCarbs,
    double TotalFiber,
    double TotalAlcohol,
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol,
    int PreMealSatietyLevel,
    int PostMealSatietyLevel,
    IReadOnlyList<ConsumptionItemResponse> Items);

public record ConsumptionItemResponse(
    Guid Id,
    Guid ConsumptionId,
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
    double? ProductAlcoholPerBase,
    Guid? RecipeId,
    string? RecipeName,
    int? RecipeServings,
    double? RecipeTotalCalories,
    double? RecipeTotalProteins,
    double? RecipeTotalFats,
    double? RecipeTotalCarbs,
    double? RecipeTotalFiber,
    double? RecipeTotalAlcohol);
