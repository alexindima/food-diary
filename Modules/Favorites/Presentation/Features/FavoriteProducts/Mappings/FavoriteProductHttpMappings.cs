using FoodDiary.Application.Favorites.FavoriteProducts.Commands.AddFavoriteProduct;
using FoodDiary.Application.Favorites.FavoriteProducts.Commands.RemoveFavoriteProduct;
using FoodDiary.Application.Favorites.FavoriteProducts.Commands.UpdateFavoriteProduct;
using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Application.Favorites.FavoriteProducts.Queries.GetFavoriteProducts;
using FoodDiary.Application.Favorites.FavoriteProducts.Queries.IsProductFavorite;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Responses;

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

    extension(FavoriteProductModel model) {
        public FavoriteProductHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.ProductId,
                    model.Name,
                    model.CreatedAtUtc,
                    model.ProductName,
                    model.Brand,
                    model.Barcode,
                    model.Comment,
                    model.ImageUrl,
                    model.CaloriesPerBase,
                    model.ProteinsPerBase,
                    model.FatsPerBase,
                    model.CarbsPerBase,
                    model.FiberPerBase,
                    model.AlcoholPerBase,
                    model.QualityScore,
                    model.QualityGrade,
                    model.IsOwnedByCurrentUser,
                    model.BaseUnit,
                    model.PreferredPortionAmount,
                    model.DefaultPortionAmount);
    }
}
