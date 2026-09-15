namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record UsdaFoodDetailModel(
    int FdcId,
    string Description,
    string? FoodCategory,
    IReadOnlyList<MicronutrientModel> Nutrients,
    IReadOnlyList<UsdaFoodPortionModel> Portions,
    HealthAreaScoresModel? HealthScores);
