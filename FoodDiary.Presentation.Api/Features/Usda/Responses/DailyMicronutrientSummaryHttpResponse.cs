namespace FoodDiary.Presentation.Api.Features.Usda.Responses;

public sealed record DailyMicronutrientSummaryHttpResponse(
    DateTime Date,
    int LinkedProductCount,
    int TotalProductCount,
    IReadOnlyList<DailyMicronutrientHttpResponse> Nutrients);

public sealed record DailyMicronutrientHttpResponse(
    int NutrientId,
    string Name,
    string Unit,
    double TotalAmount,
    double? DailyValue,
    double? PercentDailyValue);
