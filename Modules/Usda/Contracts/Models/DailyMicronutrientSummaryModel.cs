namespace FoodDiary.Application.Abstractions.Usda.Models;

public sealed record DailyMicronutrientSummaryModel(
    DateTime Date,
    int LinkedProductCount,
    int TotalProductCount,
    IReadOnlyList<DailyMicronutrientModel> Nutrients,
    HealthAreaScoresModel? HealthScores);
