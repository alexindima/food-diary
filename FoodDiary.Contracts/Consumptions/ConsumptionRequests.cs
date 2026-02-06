using System;
using System.Collections.Generic;

namespace FoodDiary.Contracts.Consumptions;

public record CreateConsumptionRequest(
    DateTime Date,
    string? MealType,
    string? Comment,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<ConsumptionItemRequest> Items,
    IReadOnlyList<ConsumptionAiSessionRequest>? AiSessions = null,
    bool IsNutritionAutoCalculated = true,
    double? ManualCalories = null,
    double? ManualProteins = null,
    double? ManualFats = null,
    double? ManualCarbs = null,
    double? ManualFiber = null,
    double? ManualAlcohol = null,
    int PreMealSatietyLevel = 0,
    int PostMealSatietyLevel = 0);

public record UpdateConsumptionRequest(
    DateTime Date,
    string? MealType,
    string? Comment,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<ConsumptionItemRequest> Items,
    IReadOnlyList<ConsumptionAiSessionRequest>? AiSessions = null,
    bool IsNutritionAutoCalculated = true,
    double? ManualCalories = null,
    double? ManualProteins = null,
    double? ManualFats = null,
    double? ManualCarbs = null,
    double? ManualFiber = null,
    double? ManualAlcohol = null,
    int PreMealSatietyLevel = 0,
    int PostMealSatietyLevel = 0);

public record ConsumptionItemRequest(
    Guid? ProductId,
    Guid? RecipeId,
    double Amount);

public record ConsumptionAiSessionRequest(
    Guid? ImageAssetId,
    DateTime? RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<ConsumptionAiItemRequest> Items);

public record ConsumptionAiItemRequest(
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
