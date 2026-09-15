using FoodDiary.Modules.Products.FoodQuality.ValueObjects;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Tests;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static class TestProductOverview {
    public static ProductOverviewReadItem From(Product product, UserId currentUserId) {
        FoodQualityScore quality = product.GetQualityScore();
        return new(
            product.Id,
            product.UserId,
            product.Barcode,
            product.Name,
            product.Brand,
            product.ProductType,
            product.Category,
            product.Description,
            product.Comment,
            product.ImageUrl,
            product.ImageAssetId,
            product.BaseUnit,
            product.BaseAmount,
            product.DefaultPortionAmount,
            product.CaloriesPerBase,
            product.ProteinsPerBase,
            product.FatsPerBase,
            product.CarbsPerBase,
            product.FiberPerBase,
            product.AlcoholPerBase,
            UsageCount: 0,
            product.Visibility,
            product.CreatedOnUtc,
            product.UserId == currentUserId,
            quality.Score,
            quality.Grade.ToString().ToLowerInvariant(),
            product.UsdaFdcId);
    }
}
