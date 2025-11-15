namespace FoodDiary.Contracts.Products;

public record UpdateProductRequest(
    string? Barcode,
    string? Name,
    string? Brand,
    string? Category,
    string? Description,
    string? Comment,
    string? ImageUrl,
    string? BaseUnit,
    double? BaseAmount,
    double? CaloriesPerBase,
    double? ProteinsPerBase,
    double? FatsPerBase,
    double? CarbsPerBase,
    double? FiberPerBase,
    string? Visibility);
