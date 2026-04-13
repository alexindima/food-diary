namespace FoodDiary.Presentation.Api.Features.FavoriteProducts.Responses;

public sealed record FavoriteProductHttpResponse(
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
