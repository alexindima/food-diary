using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.FavoriteProducts.Models;

public sealed record FavoriteProductReadModel(
    Guid Id,
    Guid ProductId,
    Guid UserId,
    string? Name,
    DateTime CreatedAtUtc,
    string ProductName,
    string? Brand,
    string? Barcode,
    string? Comment,
    string? ImageUrl,
    double CaloriesPerBase,
    double ProteinsPerBase,
    double FatsPerBase,
    double CarbsPerBase,
    double FiberPerBase,
    double AlcoholPerBase,
    ProductType ProductType,
    MeasurementUnit BaseUnit,
    double? PreferredPortionAmount,
    double DefaultPortionAmount,
    Guid ProductUserId);
