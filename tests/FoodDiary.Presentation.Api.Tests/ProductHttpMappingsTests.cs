using FoodDiary.Presentation.Api.Features.Products.Mappings;
using FoodDiary.Presentation.Api.Features.Products.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class ProductHttpMappingsTests {
    [Fact]
    public void CreateProductRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var imageAssetId = Guid.NewGuid();
        var request = new CreateProductHttpRequest(
            Barcode: "4601234567890",
            Name: "Greek Yogurt",
            Brand: "MilkCo",
            ProductType: "Packaged",
            Category: "Dairy",
            Description: "High protein yogurt",
            Comment: "Keep cold",
            ImageUrl: "https://cdn.example/yogurt.png",
            ImageAssetId: imageAssetId,
            BaseUnit: "G",
            BaseAmount: 100,
            DefaultPortionAmount: 180,
            CaloriesPerBase: 73,
            ProteinsPerBase: 9.5,
            FatsPerBase: 2.1,
            CarbsPerBase: 3.8,
            FiberPerBase: 0.0,
            AlcoholPerBase: 0.0,
            Visibility: "Private");

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(request.Barcode, command.Barcode);
        Assert.Equal(request.Name, command.Name);
        Assert.Equal(request.Brand, command.Brand);
        Assert.Equal(request.ProductType, command.ProductType);
        Assert.Equal(request.Category, command.Category);
        Assert.Equal(request.Description, command.Description);
        Assert.Equal(request.Comment, command.Comment);
        Assert.Equal(request.ImageUrl, command.ImageUrl);
        Assert.Equal(request.ImageAssetId, command.ImageAssetId);
        Assert.Equal(request.BaseUnit, command.BaseUnit);
        Assert.Equal(request.BaseAmount, command.BaseAmount);
        Assert.Equal(request.DefaultPortionAmount, command.DefaultPortionAmount);
        Assert.Equal(request.CaloriesPerBase, command.CaloriesPerBase);
        Assert.Equal(request.ProteinsPerBase, command.ProteinsPerBase);
        Assert.Equal(request.FatsPerBase, command.FatsPerBase);
        Assert.Equal(request.CarbsPerBase, command.CarbsPerBase);
        Assert.Equal(request.FiberPerBase, command.FiberPerBase);
        Assert.Equal(request.AlcoholPerBase, command.AlcoholPerBase);
        Assert.Equal(request.Visibility, command.Visibility);
    }

    [Fact]
    public void UpdateProductRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var imageAssetId = Guid.NewGuid();
        var request = new UpdateProductHttpRequest(
            Barcode: "123456",
            ClearBarcode: true,
            Name: "Updated product",
            Brand: "Updated brand",
            ClearBrand: false,
            ProductType: "Custom",
            Category: "Snacks",
            ClearCategory: true,
            Description: "Updated description",
            ClearDescription: false,
            Comment: "Updated comment",
            ClearComment: true,
            ImageUrl: "https://cdn.example/product.png",
            ClearImageUrl: false,
            ImageAssetId: imageAssetId,
            ClearImageAssetId: true,
            BaseUnit: "Ml",
            BaseAmount: 250,
            DefaultPortionAmount: 330,
            CaloriesPerBase: 120,
            ProteinsPerBase: 4.2,
            FatsPerBase: 3.1,
            CarbsPerBase: 15.4,
            FiberPerBase: 1.3,
            AlcoholPerBase: 0.2,
            Visibility: "Public");

        var command = request.ToCommand(userId, productId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(productId, command.ProductId);
        Assert.Equal(request.Barcode, command.Barcode);
        Assert.Equal(request.ClearBarcode, command.ClearBarcode);
        Assert.Equal(request.Name, command.Name);
        Assert.Equal(request.Brand, command.Brand);
        Assert.Equal(request.ClearBrand, command.ClearBrand);
        Assert.Equal(request.ProductType, command.ProductType);
        Assert.Equal(request.Category, command.Category);
        Assert.Equal(request.ClearCategory, command.ClearCategory);
        Assert.Equal(request.Description, command.Description);
        Assert.Equal(request.ClearDescription, command.ClearDescription);
        Assert.Equal(request.Comment, command.Comment);
        Assert.Equal(request.ClearComment, command.ClearComment);
        Assert.Equal(request.ImageUrl, command.ImageUrl);
        Assert.Equal(request.ClearImageUrl, command.ClearImageUrl);
        Assert.Equal(request.ImageAssetId, command.ImageAssetId);
        Assert.Equal(request.ClearImageAssetId, command.ClearImageAssetId);
        Assert.Equal(request.BaseUnit, command.BaseUnit);
        Assert.Equal(request.BaseAmount, command.BaseAmount);
        Assert.Equal(request.DefaultPortionAmount, command.DefaultPortionAmount);
        Assert.Equal(request.CaloriesPerBase, command.CaloriesPerBase);
        Assert.Equal(request.ProteinsPerBase, command.ProteinsPerBase);
        Assert.Equal(request.FatsPerBase, command.FatsPerBase);
        Assert.Equal(request.CarbsPerBase, command.CarbsPerBase);
        Assert.Equal(request.FiberPerBase, command.FiberPerBase);
        Assert.Equal(request.AlcoholPerBase, command.AlcoholPerBase);
        Assert.Equal(request.Visibility, command.Visibility);
    }
}
