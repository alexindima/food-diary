using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Products.Application.Models;

namespace FoodDiary.Modules.Products.Application.Mappings;

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
            favoriteProductId) { Images = product.Images.Select(image => new ProductImageModel(image.ImageAssetId, image.ImageUrl)).ToList() };
}
