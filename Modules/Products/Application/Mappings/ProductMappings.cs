using FoodDiary.Modules.Products.FoodQuality.ValueObjects;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Domain.Entities;

namespace FoodDiary.Modules.Products.Application.Mappings;

public static class ProductMappings {
    public static ProductModel ToModel(
        this Product product,
        int usageCount = 0,
        bool isOwnedByCurrentUser = false,
        bool isFavorite = false,
        Guid? favoriteProductId = null) {
        FoodQualityScore quality = product.GetQualityScore();
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
            product.UsdaFdcId,
            isFavorite,
            favoriteProductId
        ) { Images = GetImages(product) };
    }
    private static IReadOnlyList<ProductImageModel> GetImages(Product product) {
        if (product.Images.Count > 0) {
            return product.Images.OrderBy(image => image.Position).Select(image => new ProductImageModel(image.ImageAssetId.Value, image.ImageUrl)).ToList();
        }
        return product.ImageUrl is { } url ? [new ProductImageModel(product.ImageAssetId?.Value, url)] : [];
    }
}
