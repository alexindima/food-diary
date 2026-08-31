using FoodDiary.Application.Products.Mappings;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Tests.Products;

public partial class ProductsFeatureTests {
    [Fact]
    public void ProductMappings_ToModel_HidesOwnerCommentForNonOwnerAndPreservesFavoriteMetadata() {
        var favoriteProductId = FavoriteProductId.New();
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 120,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            comment: "Owner note",
            visibility: Visibility.Private);

        ProductModel nonOwnerModel = product.ToModel(
            usageCount: 7,
            isOwnedByCurrentUser: false,
            isFavorite: true,
            favoriteProductId: favoriteProductId.Value);
        ProductModel ownerModel = product.ToModel(isOwnedByCurrentUser: true);

        Assert.Null(nonOwnerModel.Comment);
        Assert.Equal("Owner note", ownerModel.Comment);
        Assert.Equal(7, nonOwnerModel.UsageCount);
        Assert.True(nonOwnerModel.IsFavorite);
        Assert.Equal(favoriteProductId.Value, nonOwnerModel.FavoriteProductId);
    }

}
