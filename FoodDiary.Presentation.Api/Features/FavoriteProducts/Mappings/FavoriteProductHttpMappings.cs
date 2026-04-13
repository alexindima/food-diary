using FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.FavoriteProducts.Queries.GetFavoriteProducts;
using FoodDiary.Application.FavoriteProducts.Queries.IsProductFavorite;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Responses;

namespace FoodDiary.Presentation.Api.Features.FavoriteProducts.Mappings;

public static class FavoriteProductHttpMappings {
    public static AddFavoriteProductCommand ToCommand(this AddFavoriteProductHttpRequest request, Guid userId) =>
        new(userId, request.ProductId, request.Name);

    public static RemoveFavoriteProductCommand ToDeleteCommand(this Guid id, Guid userId) =>
        new(userId, id);

    public static GetFavoriteProductsQuery ToQuery(this Guid userId) =>
        new(userId);

    public static IsProductFavoriteQuery ToIsFavoriteQuery(this Guid productId, Guid userId) =>
        new(userId, productId);

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
            model.DefaultPortionAmount);
}
