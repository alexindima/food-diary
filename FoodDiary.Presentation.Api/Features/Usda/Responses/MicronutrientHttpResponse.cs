namespace FoodDiary.Presentation.Api.Features.Usda.Responses;

public sealed record MicronutrientHttpResponse(
    int NutrientId,
    string Name,
    string Unit,
    double AmountPer100g,
    double? DailyValue,
    double? PercentDailyValue);
