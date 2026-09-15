namespace FoodDiary.Modules.Usda.Presentation.Responses;

public sealed record MicronutrientHttpResponse(
    int NutrientId,
    string Name,
    string Unit,
    double AmountPer100G,
    double? DailyValue,
    double? PercentDailyValue);
