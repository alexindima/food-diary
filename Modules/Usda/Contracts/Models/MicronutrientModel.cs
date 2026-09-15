namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record MicronutrientModel(
    int NutrientId,
    string Name,
    string Unit,
    double AmountPer100G,
    double? DailyValue,
    double? PercentDailyValue);
