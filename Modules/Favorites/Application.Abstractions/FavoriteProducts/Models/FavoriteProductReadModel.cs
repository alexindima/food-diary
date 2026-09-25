using FoodDiary.Modules.Products.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Models;

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
    Guid ProductUserId) {
    public IReadOnlyList<string> ImageUrls { get; init; } = [];
}
