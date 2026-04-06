namespace FoodDiary.Presentation.Api.Features.OpenFoodFacts.Responses;

public sealed record OpenFoodFactsProductHttpResponse(
    string Barcode,
    string Name,
    string? Brand,
    string? Category,
    string? ImageUrl,
    double? CaloriesPer100G,
    double? ProteinsPer100G,
    double? FatsPer100G,
    double? CarbsPer100G,
    double? FiberPer100G);
