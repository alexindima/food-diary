namespace FoodDiary.Application.Products.Models;

public sealed record ProductSearchSuggestionModel(
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
