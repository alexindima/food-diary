using FoodDiary.Application.Common.Models;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Commands.DeleteProduct;
using FoodDiary.Application.Products.Commands.DuplicateProduct;
using FoodDiary.Application.Products.Commands.UpdateProduct;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Products.Queries.GetProductById;
using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Application.Products.Queries.GetProductsOverview;
using FoodDiary.Application.Products.Queries.GetRecentProducts;
using FoodDiary.Application.Products.Queries.SearchProductSuggestions;
using FoodDiary.Presentation.Api.Features.Products.Mappings;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Presentation.Api.Features.Products.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProductHttpMappingsTests {
    [Fact]
    public void SearchProductSuggestionsQuery_MapsSearchAndLimit() {
        SearchProductSuggestionsQuery query = ProductHttpMappings.ToSuggestionsQuery("apple", 7);

        Assert.Equal("apple", query.Search);
        Assert.Equal(7, query.Limit);
    }

    [Fact]
    public void ProductCommands_MapProductAndUserIds() {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        DeleteProductCommand deleteCommand = productId.ToDeleteCommand(userId);
        DuplicateProductCommand duplicateCommand = productId.ToDuplicateCommand(userId);

        Assert.Equal(userId, deleteCommand.UserId);
        Assert.Equal(productId, deleteCommand.ProductId);
        Assert.Equal(userId, duplicateCommand.UserId);
        Assert.Equal(productId, duplicateCommand.ProductId);
    }

    [Fact]
    public void ProductSearchSuggestions_ToHttpResponse_MapsAllFields() {
        var model = new ProductSearchSuggestionModel(
            Source: "usda",
            Name: "Apple",
            Brand: "Farm",
            Category: "Fruit",
            Barcode: "123",
            UsdaFdcId: 456,
            ImageUrl: "https://cdn.example/apple.png",
            CaloriesPer100G: 52,
            ProteinsPer100G: 0.3,
            FatsPer100G: 0.2,
            CarbsPer100G: 14,
            FiberPer100G: 2.4);

        IReadOnlyList<ProductSearchSuggestionHttpResponse> response = new[] { model }.ToHttpResponse();

        ProductSearchSuggestionHttpResponse item = Assert.Single(response);
        Assert.Equal(model.Source, item.Source);
        Assert.Equal(model.Name, item.Name);
        Assert.Equal(model.Brand, item.Brand);
        Assert.Equal(model.Category, item.Category);
        Assert.Equal(model.Barcode, item.Barcode);
        Assert.Equal(model.UsdaFdcId, item.UsdaFdcId);
        Assert.Equal(model.ImageUrl, item.ImageUrl);
        Assert.Equal(model.CaloriesPer100G, item.CaloriesPer100G);
        Assert.Equal(model.ProteinsPer100G, item.ProteinsPer100G);
        Assert.Equal(model.FatsPer100G, item.FatsPer100G);
        Assert.Equal(model.CarbsPer100G, item.CarbsPer100G);
        Assert.Equal(model.FiberPer100G, item.FiberPer100G);
    }

    [Fact]
    public void GetProductsHttpQuery_ToQuery_NormalizesPagingSearchAndProductTypes() {
        var userId = Guid.NewGuid();
        var request = new GetProductsHttpQuery(
            Page: 0,
            Limit: 500,
            Search: "  yogurt  ",
            IncludePublic: true,
            ProductTypes: "Food, food, Drink");

        GetProductsQuery query = request.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(1, query.Page);
        Assert.Equal(100, query.Limit);
        Assert.Equal("yogurt", query.Search);
        Assert.True(query.IncludePublic);
        Assert.Equal(["Food", "Drink"], query.ProductTypes);
    }

    [Fact]
    public void GetProductsHttpQuery_ToQuery_UsesNullSearchAndProductTypesForBlankValues() {
        var request = new GetProductsHttpQuery(
            Page: 2,
            Limit: 20,
            Search: " ",
            IncludePublic: false,
            ProductTypes: " ");

        GetProductsQuery query = request.ToQuery(Guid.NewGuid());

        Assert.Equal(2, query.Page);
        Assert.Equal(20, query.Limit);
        Assert.Null(query.Search);
        Assert.Null(query.ProductTypes);
    }

    [Fact]
    public void GetProductsOverviewHttpQuery_ToQuery_NormalizesLimits() {
        var userId = Guid.NewGuid();
        var request = new GetProductsOverviewHttpQuery(
            Page: -5,
            Limit: 0,
            Search: "  bar  ",
            IncludePublic: true,
            RecentLimit: 100,
            FavoriteLimit: 0,
            ProductTypes: "Custom,Food");

        GetProductsOverviewQuery query = request.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(1, query.Page);
        Assert.Equal(1, query.Limit);
        Assert.Equal("bar", query.Search);
        Assert.True(query.IncludePublic);
        Assert.Equal(50, query.RecentLimit);
        Assert.Equal(1, query.FavoriteLimit);
        Assert.Equal(["Custom", "Food"], query.ProductTypes);
    }

    [Fact]
    public void GetRecentProductsHttpQuery_ToQuery_ClampsLimit() {
        var userId = Guid.NewGuid();
        var request = new GetRecentProductsHttpQuery(Limit: 500, IncludePublic: true);

        GetRecentProductsQuery query = request.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(50, query.Limit);
        Assert.True(query.IncludePublic);
    }

    [Fact]
    public void ProductId_ToQuery_MapsUserAndProductIds() {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        GetProductByIdQuery query = productId.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(productId, query.ProductId);
    }

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

        CreateProductCommand command = request.ToCommand(userId);

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

        UpdateProductCommand command = request.ToCommand(userId, productId);

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

    [Fact]
    public void ProductModel_ToHttpResponse_MapsFavoriteFields() {
        var favoriteProductId = Guid.NewGuid();
        var model = new ProductModel(
            Guid.NewGuid(),
            "4601234567890",
            "Greek Yogurt",
            "MilkCo",
            "Food",
            "Dairy",
            "High protein yogurt",
            "Owner note",
            "https://cdn.example/yogurt.png",
            null,
            "G",
            100,
            150,
            73,
            9.5,
            2.1,
            3.8,
            0,
            0,
            12,
            "Private",
            DateTime.UtcNow,
            true,
            88,
            "green",
            123456,
            true,
            favoriteProductId);

        ProductHttpResponse response = model.ToHttpResponse();

        Assert.Equal(model.Id, response.Id);
        Assert.True(response.IsFavorite);
        Assert.Equal(favoriteProductId, response.FavoriteProductId);
        Assert.Equal(model.QualityScore, response.QualityScore);
        Assert.Equal(model.QualityGrade, response.QualityGrade);
    }

    [Fact]
    public void ProductOverviewModel_ToHttpResponse_MapsNestedCollections() {
        var product = new ProductModel(
            Guid.NewGuid(),
            null,
            "Protein Bar",
            null,
            "Food",
            null,
            null,
            null,
            null,
            null,
            "G",
            100,
            60,
            380,
            30,
            10,
            40,
            5,
            0,
            4,
            "Private",
            DateTime.UtcNow,
            true,
            64,
            "yellow",
            null,
            false,
            null);
        var favorite = new FavoriteProductModel(
            Guid.NewGuid(),
            product.Id,
            "My bar",
            DateTime.UtcNow,
            product.Name,
            product.Brand,
            product.ImageUrl,
            product.CaloriesPerBase,
            product.BaseUnit,
            product.DefaultPortionAmount);
        var overview = new ProductOverviewModel(
            [product],
            new PagedResponse<ProductModel>([product], 1, 10, 1, 1),
            [favorite],
            1);

        ProductOverviewHttpResponse response = overview.ToHttpResponse();

        Assert.Single(response.RecentItems);
        Assert.Single(response.AllProducts.Data);
        Assert.Single(response.FavoriteItems);
        Assert.Equal(1, response.FavoriteTotalCount);
        Assert.Equal(product.Id, response.AllProducts.Data[0].Id);
        Assert.Equal(favorite.Id, response.FavoriteItems[0].Id);
    }
}
