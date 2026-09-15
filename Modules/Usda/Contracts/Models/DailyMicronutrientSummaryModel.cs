namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record DailyMicronutrientSummaryModel(
    DateTime Date,
    int LinkedProductCount,
    int TotalProductCount,
    IReadOnlyList<DailyMicronutrientModel> Nutrients,
    HealthAreaScoresModel? HealthScores);
