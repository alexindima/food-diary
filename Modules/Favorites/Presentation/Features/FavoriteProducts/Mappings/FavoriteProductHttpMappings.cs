using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Commands.AddFavoriteProduct;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Commands.UpdateFavoriteProduct;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.GetFavoriteProducts;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.IsProductFavorite;
using FoodDiary.Modules.Favorites.Presentation.Features.FavoriteProducts.Requests;

namespace FoodDiary.Modules.Favorites.Presentation.Features.FavoriteProducts.Mappings;

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
