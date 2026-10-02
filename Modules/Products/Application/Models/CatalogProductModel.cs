namespace FoodDiary.Modules.Products.Application.Models;

public sealed record CatalogProductModel(
    Guid Id,
    string Name,
    string? Barcode,
    string? Brand,
    string ProductType,
    string? Category,
    string? Description,
    string? ImageUrl,
    string BaseUnit,
    double BaseAmount,
    double DefaultPortionAmount,
    double CaloriesPerBase,
    double ProteinsPerBase,
    double FatsPerBase,
    double CarbsPerBase,
    double FiberPerBase,
    double AlcoholPerBase);
