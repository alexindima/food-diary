namespace FoodDiary.Presentation.Api.Features.Products.Responses;

public sealed record ProductSearchSuggestionHttpResponse(
    string Source,
    string Name,
    string? Brand,
    string? Category,
    string? Barcode,
    int? UsdaFdcId,
    string? ImageUrl,
    double? CaloriesPer100G,
    double? ProteinsPer100G,
    double? FatsPer100G,
    double? CarbsPer100G,
    double? FiberPer100G);
