namespace FoodDiary.Application.Abstractions.Usda.Models;

public sealed record MicronutrientModel(
    int NutrientId,
    string Name,
    string Unit,
    double AmountPer100G,
    double? DailyValue,
    double? PercentDailyValue);
