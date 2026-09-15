namespace FoodDiary.Modules.Usda.Presentation.Responses;

public sealed record UsdaFoodDetailHttpResponse(
    int FdcId,
    string Description,
    string? FoodCategory,
    IReadOnlyList<MicronutrientHttpResponse> Nutrients,
    IReadOnlyList<UsdaFoodPortionHttpResponse> Portions,
    HealthAreaScoresHttpResponse? HealthScores);
