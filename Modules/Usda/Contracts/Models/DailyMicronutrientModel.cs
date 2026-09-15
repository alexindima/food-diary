namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record DailyMicronutrientModel(
    int NutrientId,
    string Name,
    string Unit,
    double TotalAmount,
    double? DailyValue,
    double? PercentDailyValue);
