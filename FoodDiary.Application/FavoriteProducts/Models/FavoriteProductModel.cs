namespace FoodDiary.Application.FavoriteProducts.Models;

public sealed record FavoriteProductModel(
    Guid Id,
    Guid ProductId,
    string? Name,
    DateTime CreatedAtUtc,
    string ProductName,
    string? Brand,
    string? ImageUrl,
    double CaloriesPerBase,
    string BaseUnit,
    double DefaultPortionAmount);
