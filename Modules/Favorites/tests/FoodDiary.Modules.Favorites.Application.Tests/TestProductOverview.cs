using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
internal static class TestProductOverview {
    public static FavoriteProductSourceModel From(Product product, UserId currentUserId) {
        FoodQualityScore quality = product.GetQualityScore();
        return new(product.Name, product.Brand, product.Barcode, product.Comment, product.ImageUrl,
            product.CaloriesPerBase, product.ProteinsPerBase, product.FatsPerBase, product.CarbsPerBase,
            product.FiberPerBase, product.AlcoholPerBase, quality.Score, quality.Grade.ToString().ToLowerInvariant(),
            product.UserId == currentUserId, product.BaseUnit, product.DefaultPortionAmount);
    }
}
