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
            UserId: userIdValue,
            Barcode: request.Barcode,
            Name: request.Name,
            Brand: request.Brand,
            ProductType: request.ProductType,
            Category: request.Category,
            Description: request.Description,
            Comment: request.Comment,
            ImageUrl: request.ImageUrl,
            ImageAssetId: request.ImageAssetId,
            BaseUnit: request.BaseUnit,
            BaseAmount: request.BaseAmount,
            DefaultPortionAmount: request.DefaultPortionAmount,
            CaloriesPerBase: request.CaloriesPerBase,
            ProteinsPerBase: request.ProteinsPerBase,
            FatsPerBase: request.FatsPerBase,
            CarbsPerBase: request.CarbsPerBase,
            FiberPerBase: request.FiberPerBase,
            AlcoholPerBase: request.AlcoholPerBase,
            Visibility: request.Visibility
        );
    }

    public static UpdateProductCommand ToCommand(this UpdateProductHttpRequest request, Guid userIdValue, Guid productId) {
        return new UpdateProductCommand(
            UserId: userIdValue,
            ProductId: productId,
            Barcode: request.Barcode,
            ClearBarcode: request.ClearBarcode,
            Name: request.Name,
            Brand: request.Brand,
            ClearBrand: request.ClearBrand,
            ProductType: request.ProductType,
            Category: request.Category,
            ClearCategory: request.ClearCategory,
            Description: request.Description,
            ClearDescription: request.ClearDescription,
            Comment: request.Comment,
            ClearComment: request.ClearComment,
            ImageUrl: request.ImageUrl,
            ClearImageUrl: request.ClearImageUrl,
            ImageAssetId: request.ImageAssetId,
            ClearImageAssetId: request.ClearImageAssetId,
            BaseUnit: request.BaseUnit,
            BaseAmount: request.BaseAmount,
            DefaultPortionAmount: request.DefaultPortionAmount,
            CaloriesPerBase: request.CaloriesPerBase,
            ProteinsPerBase: request.ProteinsPerBase,
            FatsPerBase: request.FatsPerBase,
            CarbsPerBase: request.CarbsPerBase,
            FiberPerBase: request.FiberPerBase,
            AlcoholPerBase: request.AlcoholPerBase,
            Visibility: request.Visibility);
    }
}
