using FoodDiary.Modules.Products.Application.Mappings;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;

using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Application.Models;

namespace FoodDiary.Modules.Products.Application.Tests;

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
