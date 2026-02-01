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
    int UsageCount,
    string Visibility,
    DateTime CreatedAt,
    bool IsOwnedByCurrentUser
);
