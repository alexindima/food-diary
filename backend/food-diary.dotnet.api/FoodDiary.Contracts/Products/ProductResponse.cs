namespace FoodDiary.Contracts.Products;

public record ProductResponse(
    Guid Id,
    string? Barcode,
    string Name,
    string? Brand,
    string ProductType,
    string? Category,
    string? Description,
    string? Comment,
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
    DateTime CreatedAt,
    bool IsOwnedByCurrentUser
);
