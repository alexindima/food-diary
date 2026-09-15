namespace FoodDiary.Modules.Usda.Presentation.Responses;

public sealed record UsdaFoodHttpResponse(
    int FdcId,
    string Description,
    string? FoodCategory);
