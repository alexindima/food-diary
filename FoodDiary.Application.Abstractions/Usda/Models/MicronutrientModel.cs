namespace FoodDiary.Application.Usda.Models;

public sealed record MicronutrientModel(
    int NutrientId,
    string Name,
    string Unit,
    double AmountPer100g,
    double? DailyValue,
    double? PercentDailyValue);
