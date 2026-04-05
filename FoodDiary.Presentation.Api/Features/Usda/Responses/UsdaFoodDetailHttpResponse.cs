namespace FoodDiary.Presentation.Api.Features.Usda.Responses;

public sealed record UsdaFoodDetailHttpResponse(
    int FdcId,
    string Description,
    string? FoodCategory,
    IReadOnlyList<MicronutrientHttpResponse> Nutrients,
    IReadOnlyList<UsdaFoodPortionHttpResponse> Portions);
