using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.FavoriteProducts.Mappings;

public static class FavoriteProductMappings {
    public static FavoriteProductModel ToModel(this FavoriteProduct favorite) {
        return favorite.ToModel(favorite.Product);
    }

    public static FavoriteProductModel ToModel(this FavoriteProduct favorite, Product product) {
        FoodQualityScore quality = product.GetQualityScore();
        bool isOwnedByCurrentUser = product.UserId == favorite.UserId;

        return new(
            favorite.Id.Value,
            favorite.ProductId.Value,
            favorite.Name,
            favorite.CreatedAtUtc,
            product.Name,
            product.Brand,
            product.Barcode,
            isOwnedByCurrentUser ? product.Comment : null,
            product.ImageUrl,
            product.CaloriesPerBase,
            product.ProteinsPerBase,
            product.FatsPerBase,
            product.CarbsPerBase,
            product.FiberPerBase,
            product.AlcoholPerBase,
            quality.Score,
            quality.Grade.ToString().ToLowerInvariant(),
            isOwnedByCurrentUser,
            product.BaseUnit.ToString(),
            favorite.PreferredPortionAmount ?? product.DefaultPortionAmount,
            product.DefaultPortionAmount);
    }

    public static FavoriteProductModel ToModel(this FavoriteProductReadModel favorite) {
        var quality = FoodQualityScore.Calculate(
            favorite.CaloriesPerBase,
            favorite.ProteinsPerBase,
            favorite.FatsPerBase,
            favorite.CarbsPerBase,
            favorite.FiberPerBase,
            favorite.AlcoholPerBase,
            favorite.ProductType);
        bool isOwnedByCurrentUser = favorite.ProductUserId == favorite.UserId;

        return new(
            favorite.Id,
            favorite.ProductId,
            favorite.Name,
            favorite.CreatedAtUtc,
            favorite.ProductName,
            favorite.Brand,
            favorite.Barcode,
            isOwnedByCurrentUser ? favorite.Comment : null,
            favorite.ImageUrl,
            favorite.CaloriesPerBase,
            favorite.ProteinsPerBase,
            favorite.FatsPerBase,
            favorite.CarbsPerBase,
            favorite.FiberPerBase,
            favorite.AlcoholPerBase,
            quality.Score,
            quality.Grade.ToString().ToLowerInvariant(),
            isOwnedByCurrentUser,
            favorite.BaseUnit.ToString(),
            favorite.PreferredPortionAmount ?? favorite.DefaultPortionAmount,
            favorite.DefaultPortionAmount);
    }
}
