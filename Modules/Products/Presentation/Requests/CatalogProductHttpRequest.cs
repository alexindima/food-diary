namespace FoodDiary.Modules.Products.Presentation.Requests;

public sealed record CatalogProductHttpRequest(
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
