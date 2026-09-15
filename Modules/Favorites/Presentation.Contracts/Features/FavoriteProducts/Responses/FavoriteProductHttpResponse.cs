namespace FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteProducts.Responses;

public sealed record FavoriteProductHttpResponse(
    Guid Id,
    Guid ProductId,
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
    int QualityScore,
    string QualityGrade,
    bool IsOwnedByCurrentUser,
    string BaseUnit,
    double PreferredPortionAmount,
    double DefaultPortionAmount);
