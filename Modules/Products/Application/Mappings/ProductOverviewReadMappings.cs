using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Mappings;

public static class ProductOverviewReadMappings {
    public static ProductModel ToModel(
        this ProductOverviewReadItem product,
        bool isFavorite = false,
        Guid? favoriteProductId = null) =>
        new(
            product.Id.Value,
            product.Barcode,
            product.Name,
            product.Brand,
            product.ProductType.ToString(),
            product.Category,
            product.Description,
            product.Comment,
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
            product.UsageCount,
            product.Visibility.ToString(),
            product.CreatedOnUtc,
            product.IsOwnedByCurrentUser,
            product.QualityScore,
            product.QualityGrade,
            product.UsdaFdcId,
            isFavorite,
            favoriteProductId);
}
