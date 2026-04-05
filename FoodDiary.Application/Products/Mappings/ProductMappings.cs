using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.Products.Mappings;

public static class ProductMappings {
    public static ProductModel ToModel(this Product product, int usageCount = 0, bool isOwnedByCurrentUser = false) {
        var quality = product.GetQualityScore();
        return new ProductModel(
            product.Id.Value,
            product.Barcode,
            product.Name,
            product.Brand,
            product.ProductType.ToString(),
            product.Category,
            product.Description,
            isOwnedByCurrentUser ? product.Comment : null,
            product.ImageUrl,
            product.ImageAssetId?.Value,
            product.BaseUnit.ToString(),
            product.BaseAmount,
            product.DefaultPortionAmount,
            product.CaloriesPerBase,
            product.ProteinsPerBase,
            product.FatsPerBase,
            product.CarbsPerBase,
            product.FiberPerBase,
            product.AlcoholPerBase,
            usageCount,
            product.Visibility.ToString(),
            product.CreatedOnUtc,
            isOwnedByCurrentUser,
            quality.Score,
            quality.Grade.ToString().ToLowerInvariant(),
            product.UsdaFdcId
        );
    }
}
