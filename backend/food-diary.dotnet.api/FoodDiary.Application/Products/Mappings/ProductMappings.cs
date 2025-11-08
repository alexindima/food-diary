using FoodDiary.Application.Products.Commands.CreateProduct;
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
    public static ProductResponse ToResponse(this Product product, int usageCount = 0)
    {
        return new ProductResponse(
            product.Id.Value,
            product.Barcode,
            product.Name,
            product.Brand,
            product.Category,
            product.Description,
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
            product.CreatedOnUtc
        );
    }

    /// <summary>
    /// Маппинг CreateProductRequest -> CreateProductCommand
    /// </summary>
    public static CreateProductCommand ToCommand(this CreateProductRequest request, Guid userIdValue)
    {
        return new CreateProductCommand(
            new UserId(userIdValue),
            request.Barcode,
            request.Name,
            request.Brand,
            request.Category,
            request.Description,
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
}
