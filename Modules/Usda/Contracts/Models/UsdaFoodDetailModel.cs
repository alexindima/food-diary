namespace FoodDiary.Application.Abstractions.Usda.Models;

public sealed record UsdaFoodDetailModel(
    int FdcId,
    string Description,
    string? FoodCategory,
    IReadOnlyList<MicronutrientModel> Nutrients,
    IReadOnlyList<UsdaFoodPortionModel> Portions,
    HealthAreaScoresModel? HealthScores);
