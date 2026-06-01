namespace FoodDiary.Application.Abstractions.Usda.Models;

public sealed record DailyMicronutrientModel(
    int NutrientId,
    string Name,
    string Unit,
    double TotalAmount,
    double? DailyValue,
    double? PercentDailyValue);
