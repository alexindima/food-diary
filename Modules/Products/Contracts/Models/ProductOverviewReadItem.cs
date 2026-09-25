using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Contracts.Models;

public sealed record ProductOverviewReadItem(
    ProductId Id,
    UserId UserId,
    string? Barcode,
    string Name,
    string? Brand,
    ProductType ProductType,
    string? Category,
    string? Description,
    string? Comment,
    string? ImageUrl,
    ImageAssetId? ImageAssetId,
    MeasurementUnit BaseUnit,
    double BaseAmount,
    double DefaultPortionAmount,
    double CaloriesPerBase,
    double ProteinsPerBase,
    double FatsPerBase,
    double CarbsPerBase,
    double FiberPerBase,
    double AlcoholPerBase,
    int UsageCount,
    Visibility Visibility,
    DateTime CreatedOnUtc,
    bool IsOwnedByCurrentUser,
    int QualityScore,
    string QualityGrade,
    int? UsdaFdcId) {
    public IReadOnlyList<ProductImageReadItem> Images { get; init; } = [];
}
