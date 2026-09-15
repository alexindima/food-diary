namespace FoodDiary.Modules.Usda.Presentation.Responses;

public sealed record DailyMicronutrientHttpResponse(
    int NutrientId,
    string Name,
    string Unit,
    double TotalAmount,
    double? DailyValue,
    double? PercentDailyValue);
