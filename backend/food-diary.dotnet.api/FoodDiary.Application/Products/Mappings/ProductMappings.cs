using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Commands.UpdateProduct;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Mappings;

/// <summary>
/// Extension methods для маппинга Product
/// </summary>
public static class ProductMappings
{
    /// <summary>
    /// Маппинг Product -> ProductResponse (с UsageCount)
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
            product.Comment,
            product.ImageUrl,
            product.BaseUnit.ToString(),
            product.BaseAmount,
            product.CaloriesPerBase,
            product.ProteinsPerBase,
            product.FatsPerBase,
            product.CarbsPerBase,
            product.FiberPerBase,
            usageCount,
            product.Visibility.ToString(),
            product.CreatedOnUtc,
            isOwnedByCurrentUser
        );
    }

    /// <summary>
    /// Маппинг CreateProductRequest -> CreateProductCommand
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
            request.BaseUnit,
            request.BaseAmount,
            request.CaloriesPerBase,
            request.ProteinsPerBase,
            request.FatsPerBase,
            request.CarbsPerBase,
            request.FiberPerBase,
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
            request.BaseUnit,
            request.BaseAmount,
            request.CaloriesPerBase,
            request.ProteinsPerBase,
            request.FatsPerBase,
            request.CarbsPerBase,
            request.FiberPerBase,
            request.Visibility);
    }
}
