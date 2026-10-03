namespace FoodDiary.Modules.Products.Presentation.Responses;

public sealed record CatalogProductHttpResponse(
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
