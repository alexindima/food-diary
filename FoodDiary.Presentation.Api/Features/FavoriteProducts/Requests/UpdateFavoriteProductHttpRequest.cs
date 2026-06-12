namespace FoodDiary.Presentation.Api.Features.FavoriteProducts.Requests;

public sealed record UpdateFavoriteProductHttpRequest(string? Name, double PreferredPortionAmount);
