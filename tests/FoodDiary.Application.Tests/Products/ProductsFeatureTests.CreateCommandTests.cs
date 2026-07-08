using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Tests.Products;

public partial class ProductsFeatureTests {

    [Fact]
    public async Task CreateProductCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new CreateProductCommandHandler(new NoopProductRepository(), new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);
        var command = new CreateProductCommand(
            UserId: null,
            Barcode: null,
            Name: "Apple",
            Brand: null,
            ProductType: "Unknown",
            Category: null,
            Description: null,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            BaseUnit: "G",
            BaseAmount: 100,
            DefaultPortionAmount: 100,
            CaloriesPerBase: 52,
            ProteinsPerBase: 0.3,
            FatsPerBase: 0.2,
            CarbsPerBase: 14,
            FiberPerBase: 2.4,
            AlcoholPerBase: 0,
            Visibility: "Private");

        Result<ProductModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task CreateProductCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var handler = new CreateProductCommandHandler(new NoopProductRepository(), new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);
        var command = new CreateProductCommand(
            UserId: Guid.NewGuid(),
            Barcode: null,
            Name: "Apple",
            Brand: null,
            ProductType: "Unknown",
            Category: null,
            Description: null,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: Guid.Empty,
            BaseUnit: "G",
            BaseAmount: 100,
            DefaultPortionAmount: 100,
            CaloriesPerBase: 52,
            ProteinsPerBase: 0.3,
            FatsPerBase: 0.2,
            CarbsPerBase: 14,
            FiberPerBase: 2.4,
            AlcoholPerBase: 0,
            Visibility: "Private");

        Result<ProductModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.Ordinal);
    }


    [Fact]
    public async Task CreateProductCommandHandler_WithInvalidProductType_ReturnsValidationFailure() {
        var user = User.Create("create-product-invalid-type@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new CreateProductCommand(
                UserId: user.Id.Value,
                Barcode: null,
                Name: "Apple",
                Brand: null,
                ProductType: "NotAProductType",
                Category: null,
                Description: null,
                Comment: null,
                ImageUrl: null,
                ImageAssetId: null,
                BaseUnit: "G",
                BaseAmount: 100,
                DefaultPortionAmount: 100,
                CaloriesPerBase: 52,
                ProteinsPerBase: 0.3,
                FatsPerBase: 0.2,
                CarbsPerBase: 14,
                FiberPerBase: 2.4,
                AlcoholPerBase: 0,
                Visibility: "Private"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }


    [Fact]
    public async Task CreateProductCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("create-product-deleted-user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateProductCommand(user.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }


    [Fact]
    public async Task CreateProductCommandHandler_WithInvalidBaseUnit_ReturnsValidationFailure() {
        var user = User.Create("create-product-invalid-unit@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateProductCommand(user.Id.Value, baseUnit: "Cup"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }


    [Fact]
    public async Task CreateProductCommandHandler_WithInvalidVisibility_ReturnsValidationFailure() {
        var user = User.Create("create-product-invalid-visibility@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateProductCommand(user.Id.Value, visibility: "Shared"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }


    [Fact]
    public async Task CreateProductCommandHandler_WithUndefinedNumericProductType_ReturnsValidationFailure() {
        var user = User.Create("create-product-undefined-type@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateProductCommand(user.Id.Value, productType: "999"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }


    [Fact]
    public async Task CreateProductCommandHandler_WithValidCommand_PersistsAndReturnsOwnedModel() {
        var user = User.Create("create-product@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new CreateProductCommand(
                user.Id.Value,
                "4601234567890",
                "Apple",
                "Farm",
                "Other",
                "Fruit",
                "Fresh apple",
                "Owner note",
                "https://cdn.test/apple.png",
                ImageAssetId: null,
                "G",
                100,
                120,
                52,
                0.3,
                0.2,
                14,
                2.4,
                0,
                "Private"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.LastAddedProduct);
        Assert.Equal("Apple", repository.LastAddedProduct.Name);
        Assert.Equal("Farm", repository.LastAddedProduct.Brand);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Equal("Owner note", result.Value.Comment);
    }


    [Fact]
    public async Task CreateProductCommandHandler_WithImageAsset_UsesResolvedAssetUrl() {
        var user = User.Create("create-product-image@example.com", "hash");
        var assetId = ImageAssetId.New();
        RecordingImageAssetAccessService access = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithAsset(assetId, "https://cdn.test/assets/product.webp");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), access);

        Result<ProductModel> result = await handler.Handle(
            new CreateProductCommand(
                user.Id.Value,
                Barcode: null,
                Name: "Apple",
                Brand: null,
                ProductType: "Other",
                Category: null,
                Description: null,
                Comment: null,
                ImageUrl: "https://client.test/not-trusted.webp",
                ImageAssetId: assetId.Value,
                BaseUnit: "G",
                BaseAmount: 100,
                DefaultPortionAmount: 100,
                CaloriesPerBase: 52,
                ProteinsPerBase: 0.3,
                FatsPerBase: 0.2,
                CarbsPerBase: 14,
                FiberPerBase: 2.4,
                AlcoholPerBase: 0,
                Visibility: "Private"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal([assetId], access.RequestedAssetIds);
        Assert.Equal("https://cdn.test/assets/product.webp", repository.LastAddedProduct!.ImageUrl);
        Assert.Equal(assetId, repository.LastAddedProduct.ImageAssetId);
    }


    [Fact]
    public async Task CreateProductCommandHandler_WhenImageAssetAccessFails_DoesNotPersist() {
        var user = User.Create("create-product-forbidden-image@example.com", "hash");
        var repository = new NoopProductRepository();
        RecordingImageAssetAccessService access = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.Forbidden());
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), access);

        Result<ProductModel> result = await handler.Handle(
            new CreateProductCommand(
                user.Id.Value,
                Barcode: null,
                Name: "Apple",
                Brand: null,
                ProductType: "Other",
                Category: null,
                Description: null,
                Comment: null,
                ImageUrl: null,
                ImageAssetId: Guid.NewGuid(),
                BaseUnit: "G",
                BaseAmount: 100,
                DefaultPortionAmount: 100,
                CaloriesPerBase: 52,
                ProteinsPerBase: 0.3,
                FatsPerBase: 0.2,
                CarbsPerBase: 14,
                FiberPerBase: 2.4,
                AlcoholPerBase: 0,
                Visibility: "Private"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }

}
