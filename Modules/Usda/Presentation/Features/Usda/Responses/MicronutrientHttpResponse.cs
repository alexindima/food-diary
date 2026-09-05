namespace FoodDiary.Presentation.Api.Features.Usda.Responses;

public sealed record MicronutrientHttpResponse(
    int NutrientId,
    string Name,
    string Unit,
    double AmountPer100G,
    double? DailyValue,
    double? PercentDailyValue);
