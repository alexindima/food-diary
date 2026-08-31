using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Products.Models;

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
    int? UsdaFdcId);
