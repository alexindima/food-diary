using FoodDiary.Application.Favorites.FavoriteProducts.Commands.AddFavoriteProduct;
using FoodDiary.Application.Favorites.FavoriteProducts.Commands.RemoveFavoriteProduct;
using FoodDiary.Application.Favorites.FavoriteProducts.Commands.UpdateFavoriteProduct;
using FoodDiary.Application.Favorites.FavoriteProducts.Queries.GetFavoriteProducts;
using FoodDiary.Application.Favorites.FavoriteProducts.Queries.IsProductFavorite;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Requests;

namespace FoodDiary.Presentation.Api.Features.FavoriteProducts.Mappings;

public static class FavoriteProductHttpMappings {
    extension(AddFavoriteProductHttpRequest request) {
        public AddFavoriteProductCommand ToCommand(Guid userId) =>
                new(userId, request.ProductId, request.Name, request.PreferredPortionAmount);
    }

    extension(UpdateFavoriteProductHttpRequest request) {
        public UpdateFavoriteProductCommand ToCommand(Guid userId, Guid favoriteProductId) =>
                new(userId, favoriteProductId, request.Name, request.PreferredPortionAmount);
    }

    extension(Guid id) {
        public RemoveFavoriteProductCommand ToDeleteCommand(Guid userId) =>
            new(userId, id);
        public GetFavoriteProductsQuery ToQuery() =>
            new(id);
        public IsProductFavoriteQuery ToIsFavoriteQuery(Guid userId) =>
            new(userId, id);
    }
}
