using FoodDiary.Contracts.Products;
using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.Products.Mappings;

public static class ProductMappings {
    public static ProductResponse ToResponse(this Product product, int usageCount = 0, bool isOwnedByCurrentUser = false) {
        return new ProductResponse(
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
            isOwnedByCurrentUser
        );
    }
}
