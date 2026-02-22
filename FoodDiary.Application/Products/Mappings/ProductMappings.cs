using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Commands.UpdateProduct;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Mappings;

/// <summary>
/// Mapping helpers for product contracts and commands.
/// </summary>
public static class ProductMappings
{
    /// <summary>
    /// Maps Product to ProductResponse with optional usage and ownership flags.
    /// </summary>
    public static ProductResponse ToResponse(this Product product, int usageCount = 0, bool isOwnedByCurrentUser = false)
    {
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

    /// <summary>
    /// Maps CreateProductRequest to CreateProductCommand.
    /// </summary>
    public static CreateProductCommand ToCommand(this CreateProductRequest request, Guid? userIdValue)
    {
        return new CreateProductCommand(
            userIdValue.HasValue ? new UserId(userIdValue.Value) : null,
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

    public static UpdateProductCommand ToCommand(this UpdateProductRequest request, Guid? userIdValue, Guid productId)
    {
        return new UpdateProductCommand(
            userIdValue.HasValue ? new UserId(userIdValue.Value) : null,
            new ProductId(productId),
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
            request.Visibility);
    }
}
