namespace FoodDiary.Presentation.Api.Features.Usda.Responses;

public sealed record UsdaFoodHttpResponse(
    int FdcId,
    string Description,
    string? FoodCategory);
