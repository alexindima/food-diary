using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Infrastructure.Persistence;

internal sealed record DashboardMealProjection(
    MealId MealId,
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
    int PostMealSatietyLevel);
