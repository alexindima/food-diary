using FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Commands.UpdateFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.FavoriteProducts.Queries.GetFavoriteProducts;
using FoodDiary.Application.FavoriteProducts.Queries.IsProductFavorite;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Responses;

namespace FoodDiary.Presentation.Api.Features.FavoriteProducts.Mappings;

public static class FavoriteProductHttpMappings {
    public static AddFavoriteProductCommand ToCommand(this AddFavoriteProductHttpRequest request, Guid userId) =>
        new(userId, request.ProductId, request.Name, request.PreferredPortionAmount);

    public static UpdateFavoriteProductCommand ToCommand(this UpdateFavoriteProductHttpRequest request, Guid userId, Guid favoriteProductId) =>
        new(userId, favoriteProductId, request.Name, request.PreferredPortionAmount);

    extension(Guid id) {
        public RemoveFavoriteProductCommand ToDeleteCommand(Guid userId) =>
            new(userId, id);
        public GetFavoriteProductsQuery ToQuery() =>
            new(id);
        public IsProductFavoriteQuery ToIsFavoriteQuery(Guid userId) =>
            new(userId, id);
    }

    public static FavoriteProductHttpResponse ToHttpResponse(this FavoriteProductModel model) =>
        new(
            model.Id,
            model.ProductId,
            model.Name,
            model.CreatedAtUtc,
            model.ProductName,
            model.Brand,
            model.ImageUrl,
            model.CaloriesPerBase,
            model.BaseUnit,
            model.PreferredPortionAmount,
            model.DefaultPortionAmount);
}
