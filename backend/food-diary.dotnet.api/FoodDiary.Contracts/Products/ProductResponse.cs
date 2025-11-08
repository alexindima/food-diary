namespace FoodDiary.Contracts.Products;

public record ProductResponse(
    Guid Id,
    string? Barcode,
    string Name,
    string? Brand,
    string? Category,
    string? Description,
    string? ImageUrl,
    string BaseUnit,
    double BaseAmount,
    double CaloriesPerBase,
    double ProteinsPerBase,
    double FatsPerBase,
    double CarbsPerBase,
    double FiberPerBase,
    int UsageCount,
    string Visibility,
    DateTime CreatedAt
);
