namespace FoodDiary.Presentation.Api.Features.Usda.Responses;

public sealed record UsdaFoodPortionHttpResponse(
    int Id,
    double Amount,
    string MeasureUnitName,
    double GramWeight,
    string? PortionDescription,
    string? Modifier);
