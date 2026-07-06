using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Meals.Models;

public sealed record MealConsumptionReadModel(
    Guid Id,
    DateTime Date,
    MealType? MealType,
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
    IReadOnlyList<MealConsumptionItemReadModel> Items,
    IReadOnlyList<MealConsumptionAiSessionReadModel> AiSessions);
