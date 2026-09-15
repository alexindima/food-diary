using FoodDiary.Domain.Enums;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;

public sealed record FavoriteProductSourceModel(
    string Name,
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
    int QualityScore,
    string QualityGrade,
    bool IsOwnedByCurrentUser,
    MeasurementUnit BaseUnit,
    double DefaultPortionAmount);
