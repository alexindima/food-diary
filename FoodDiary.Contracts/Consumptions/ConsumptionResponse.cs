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
    IReadOnlyList<ConsumptionItemResponse> Items,
    IReadOnlyList<ConsumptionAiSessionResponse> AiSessions);

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

public record ConsumptionAiSessionResponse(
    Guid Id,
    Guid ConsumptionId,
    Guid? ImageAssetId,
    string? ImageUrl,
    DateTime RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<ConsumptionAiItemResponse> Items);

public record ConsumptionAiItemResponse(
    Guid Id,
    Guid SessionId,
    string NameEn,
    string? NameLocal,
    double Amount,
    string Unit,
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol);
