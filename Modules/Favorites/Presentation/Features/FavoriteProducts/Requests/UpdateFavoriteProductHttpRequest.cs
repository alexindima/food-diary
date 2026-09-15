namespace FoodDiary.Modules.Favorites.Presentation.Features.FavoriteProducts.Requests;

public sealed record UpdateFavoriteProductHttpRequest(string? Name, double PreferredPortionAmount);
