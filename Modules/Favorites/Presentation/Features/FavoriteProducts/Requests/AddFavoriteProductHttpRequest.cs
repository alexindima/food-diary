namespace FoodDiary.Modules.Favorites.Presentation.Features.FavoriteProducts.Requests;

public sealed record AddFavoriteProductHttpRequest(Guid ProductId, string? Name = null, double? PreferredPortionAmount = null);
