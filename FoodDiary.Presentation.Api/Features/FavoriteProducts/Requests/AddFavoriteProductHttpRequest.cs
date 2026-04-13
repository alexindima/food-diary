namespace FoodDiary.Presentation.Api.Features.FavoriteProducts.Requests;

public sealed record AddFavoriteProductHttpRequest(Guid ProductId, string? Name = null);
