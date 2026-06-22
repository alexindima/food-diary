using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.FavoriteProducts.Mappings;

public static class FavoriteProductMappings {
    public static FavoriteProductModel ToModel(this FavoriteProduct favorite) {
        FoodQualityScore quality = favorite.Product.GetQualityScore();
        bool isOwnedByCurrentUser = favorite.Product.UserId == favorite.UserId;

        return new(
            favorite.Id.Value,
            favorite.ProductId.Value,
            favorite.Name,
            favorite.CreatedAtUtc,
            favorite.Product.Name,
            favorite.Product.Brand,
            favorite.Product.Barcode,
            isOwnedByCurrentUser ? favorite.Product.Comment : null,
            favorite.Product.ImageUrl,
            favorite.Product.CaloriesPerBase,
            favorite.Product.ProteinsPerBase,
            favorite.Product.FatsPerBase,
            favorite.Product.CarbsPerBase,
            favorite.Product.FiberPerBase,
            favorite.Product.AlcoholPerBase,
            quality.Score,
            quality.Grade.ToString().ToLowerInvariant(),
            isOwnedByCurrentUser,
            favorite.Product.BaseUnit.ToString(),
            favorite.PreferredPortionAmount ?? favorite.Product.DefaultPortionAmount,
            favorite.Product.DefaultPortionAmount);
    }
}
