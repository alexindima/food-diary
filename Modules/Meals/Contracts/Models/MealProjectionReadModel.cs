using FoodDiary.Modules.Meals.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Meals.Contracts.Models;

public sealed record MealProjectionReadModel(
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
    IReadOnlyList<MealItemProjectionReadModel> Items,
    IReadOnlyList<MealAiSessionProjectionReadModel> AiSessions);
