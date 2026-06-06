namespace FoodDiary.Presentation.Api.Features.Usda.Responses;

public sealed record DailyMicronutrientSummaryHttpResponse(
    DateTime Date,
    int LinkedProductCount,
    int TotalProductCount,
    IReadOnlyList<DailyMicronutrientHttpResponse> Nutrients,
    HealthAreaScoresHttpResponse? HealthScores);
