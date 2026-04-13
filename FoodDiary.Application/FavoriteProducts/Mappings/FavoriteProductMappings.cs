using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Domain.Entities.FavoriteProducts;

namespace FoodDiary.Application.FavoriteProducts.Mappings;

public static class FavoriteProductMappings {
    public static FavoriteProductModel ToModel(this FavoriteProduct favorite) =>
        new(
            favorite.Id.Value,
            favorite.ProductId.Value,
            favorite.Name,
            favorite.CreatedAtUtc,
            favorite.Product.Name,
            favorite.Product.Brand,
            favorite.Product.ImageUrl,
            favorite.Product.CaloriesPerBase,
            favorite.Product.BaseUnit.ToString(),
            favorite.Product.DefaultPortionAmount);
}
