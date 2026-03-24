using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Commands.DeleteProduct;
using FoodDiary.Application.Products.Commands.DuplicateProduct;
using FoodDiary.Application.Products.Commands.UpdateProduct;
using FoodDiary.Presentation.Api.Features.Products.Requests;

namespace FoodDiary.Presentation.Api.Features.Products.Mappings;

public static class ProductHttpMappings {
    public static DeleteProductCommand ToDeleteCommand(this Guid productId, Guid userId) =>
        new(userId, productId);

    public static DuplicateProductCommand ToDuplicateCommand(this Guid productId, Guid userId) =>
        new(userId, productId);

    public static CreateProductCommand ToCommand(this CreateProductHttpRequest request, Guid userIdValue) {
        return new CreateProductCommand(
            userIdValue,
            request.Barcode,
            request.Name,
            request.Brand,
            request.ProductType,
            request.Category,
            request.Description,
            request.Comment,
            request.ImageUrl,
            request.ImageAssetId,
            request.BaseUnit,
            request.BaseAmount,
            request.DefaultPortionAmount,
            request.CaloriesPerBase,
            request.ProteinsPerBase,
            request.FatsPerBase,
            request.CarbsPerBase,
            request.FiberPerBase,
            request.AlcoholPerBase,
            request.Visibility
        );
    }

    public static UpdateProductCommand ToCommand(this UpdateProductHttpRequest request, Guid userIdValue, Guid productId) {
        return new UpdateProductCommand(
            userIdValue,
            productId,
            request.Barcode,
            request.ClearBarcode,
            request.Name,
            request.Brand,
            request.ClearBrand,
            request.ProductType,
            request.Category,
            request.ClearCategory,
            request.Description,
            request.ClearDescription,
            request.Comment,
            request.ClearComment,
            request.ImageUrl,
            request.ClearImageUrl,
            request.ImageAssetId,
            request.ClearImageAssetId,
            request.BaseUnit,
            request.BaseAmount,
            request.DefaultPortionAmount,
            request.CaloriesPerBase,
            request.ProteinsPerBase,
            request.FatsPerBase,
            request.CarbsPerBase,
            request.FiberPerBase,
            request.AlcoholPerBase,
            request.Visibility);
    }
}
