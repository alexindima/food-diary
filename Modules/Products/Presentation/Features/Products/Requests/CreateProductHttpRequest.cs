namespace FoodDiary.Presentation.Api.Features.Products.Requests;

public sealed record CreateProductHttpRequest(
    string? Barcode,
    string Name,
    string? Brand,
    string ProductType,
    string? Category,
    string? Description,
    string? Comment,
    string? ImageUrl,
    Guid? ImageAssetId,
    string BaseUnit,
    double BaseAmount,
    double DefaultPortionAmount,
    double CaloriesPerBase,
    double ProteinsPerBase,
    double FatsPerBase,
    double CarbsPerBase,
    double FiberPerBase,
    double AlcoholPerBase,
    string Visibility
);
