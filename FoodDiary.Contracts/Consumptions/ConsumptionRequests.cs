using System;
using System.Collections.Generic;

namespace FoodDiary.Contracts.Consumptions;

public record CreateConsumptionRequest(
    DateTime Date,
    string? MealType,
    string? Comment,
    IReadOnlyList<ConsumptionItemRequest> Items,
    bool IsNutritionAutoCalculated = true,
    double? ManualCalories = null,
    double? ManualProteins = null,
    double? ManualFats = null,
    double? ManualCarbs = null,
    double? ManualFiber = null,
    int PreMealSatietyLevel = 0,
    int PostMealSatietyLevel = 0);

public record UpdateConsumptionRequest(
    DateTime Date,
    string? MealType,
    string? Comment,
    IReadOnlyList<ConsumptionItemRequest> Items,
    bool IsNutritionAutoCalculated = true,
    double? ManualCalories = null,
    double? ManualProteins = null,
    double? ManualFats = null,
    double? ManualCarbs = null,
    double? ManualFiber = null,
    int PreMealSatietyLevel = 0,
    int PostMealSatietyLevel = 0);

public record ConsumptionItemRequest(
    Guid? ProductId,
    Guid? RecipeId,
    double Amount);
