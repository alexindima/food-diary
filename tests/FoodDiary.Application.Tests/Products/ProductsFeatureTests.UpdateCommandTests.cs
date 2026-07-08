using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Products.Commands.UpdateProduct;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Tests.Products;

public partial class ProductsFeatureTests {

    [Fact]
    public async Task UpdateProductCommandHandler_WhenCleanupFails_StillReturnsSuccessAndUpdatesProduct() {
        var userId = UserId.New();
        var oldAssetId = ImageAssetId.New();
        var newAssetId = ImageAssetId.New();
        var product = Product.Create(
            userId,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            imageAssetId: oldAssetId,
            visibility: Visibility.Private);

        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService("storage_error");
        var handler = new UpdateProductCommandHandler(repository, repository, cleanup, new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        var command = new UpdateProductCommand(
            userId.Value,
            product.Id.Value,
            Barcode: null,
            ClearBarcode: false,
            Name: null,
            Brand: null,
            ClearBrand: false,
            ProductType: null,
            Category: null,
            ClearCategory: false,
            Description: null,
            ClearDescription: false,
            Comment: null,
            ClearComment: false,
            ImageUrl: null,
            ClearImageUrl: false,
            ImageAssetId: newAssetId.Value,
            ClearImageAssetId: false,
            BaseUnit: null,
            BaseAmount: null,
            DefaultPortionAmount: null,
            CaloriesPerBase: null,
            ProteinsPerBase: null,
            FatsPerBase: null,
            CarbsPerBase: null,
            FiberPerBase: null,
            AlcoholPerBase: null,
            Visibility: null);

        Result<ProductModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.GetByIdForUpdateCalled);
        Assert.True(repository.UpdateCalled);
        Assert.Equal(newAssetId, product.ImageAssetId);
        Assert.Equal([oldAssetId], cleanup.RequestedAssetIds);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WithAllUpdateSections_AppliesChangesAndCleansOldAsset() {
        var user = User.Create("update-product-all@example.com", "hash");
        var oldAssetId = ImageAssetId.New();
        var newAssetId = ImageAssetId.New();
        var product = Product.Create(
            user.Id,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            barcode: "111",
            brand: "Old brand",
            productType: ProductType.Fruit,
            category: "Fruit",
            description: "Old description",
            comment: "Old comment",
            imageUrl: "https://cdn.example/old.jpg",
            imageAssetId: oldAssetId,
            visibility: Visibility.Private);
        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService();
        var handler = new UpdateProductCommandHandler(
            repository,
            repository,
            cleanup,
            new StubUserRepository(user),
            new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
                .WithAsset(newAssetId, "https://cdn.example/new.jpg"));

        Result<ProductModel> result = await handler.Handle(
            CreateFullProductUpdateCommand(user.Id.Value, product.Id.Value, newAssetId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.UpdateCalled);
        Assert.Equal("Updated apple", result.Value.Name);
        Assert.Equal("222", result.Value.Barcode);
        Assert.Null(result.Value.Brand);
        Assert.Null(result.Value.Category);
        Assert.Equal("Fresh description", result.Value.Description);
        Assert.Null(result.Value.Comment);
        Assert.Equal(newAssetId.Value, result.Value.ImageAssetId);
        Assert.Equal("https://cdn.example/new.jpg", result.Value.ImageUrl);
        Assert.Equal(nameof(MeasurementUnit.Pcs), result.Value.BaseUnit);
        Assert.Equal(2, result.Value.DefaultPortionAmount);
        Assert.Equal(80, result.Value.CaloriesPerBase);
        Assert.Equal(nameof(Visibility.Public), result.Value.Visibility);
        Assert.Equal([oldAssetId], cleanup.RequestedAssetIds);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var product = Product.Create(
            userId,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            visibility: Visibility.Private);

        var repository = new SingleProductRepository(product);
        var handler = new UpdateProductCommandHandler(
            repository,
            repository,
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new UpdateProductCommand(
                userId.Value,
                product.Id.Value,
                Barcode: null,
                ClearBarcode: false,
                Name: null,
                Brand: null,
                ClearBrand: false,
                ProductType: null,
                Category: null,
                ClearCategory: false,
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: Guid.Empty,
                ClearImageAssetId: false,
                BaseUnit: null,
                BaseAmount: null,
                DefaultPortionAmount: null,
                CaloriesPerBase: null,
                ProteinsPerBase: null,
                FatsPerBase: null,
                CarbsPerBase: null,
                FiberPerBase: null,
                AlcoholPerBase: null,
                Visibility: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.Ordinal);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new UpdateProductCommand(
                Guid.NewGuid(),
                Guid.Empty,
                Barcode: null,
                ClearBarcode: false,
                Name: null,
                Brand: null,
                ClearBrand: false,
                ProductType: null,
                Category: null,
                ClearCategory: false,
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                BaseUnit: null,
                BaseAmount: null,
                DefaultPortionAmount: null,
                CaloriesPerBase: null,
                ProteinsPerBase: null,
                FatsPerBase: null,
                CarbsPerBase: null,
                FiberPerBase: null,
                AlcoholPerBase: null,
                Visibility: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WithInvalidProductType_ReturnsValidationFailure() {
        var user = User.Create("update-product-invalid-type@example.com", "hash");
        var product = Product.Create(
            user.Id,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        var repository = new SingleProductRepository(product);
        var handler = new UpdateProductCommandHandler(
            repository,
            repository,
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new UpdateProductCommand(
                user.Id.Value,
                product.Id.Value,
                Barcode: null,
                ClearBarcode: false,
                Name: null,
                Brand: null,
                ClearBrand: false,
                ProductType: "NotAProductType",
                Category: null,
                ClearCategory: false,
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                BaseUnit: null,
                BaseAmount: null,
                DefaultPortionAmount: null,
                CaloriesPerBase: null,
                ProteinsPerBase: null,
                FatsPerBase: null,
                CarbsPerBase: null,
                FiberPerBase: null,
                AlcoholPerBase: null,
                Visibility: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(userId: null, ProductId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("update-product-deleted-user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, ProductId.New().Value, name: "Updated"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WithInvalidBaseUnit_ReturnsValidationFailure() {
        var user = User.Create("update-product-invalid-unit@example.com", "hash");
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, ProductId.New().Value, baseUnit: "Cup"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("BaseUnit", result.Error.Message, StringComparison.Ordinal);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WithInvalidVisibility_ReturnsValidationFailure() {
        var user = User.Create("update-product-invalid-visibility@example.com", "hash");
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, ProductId.New().Value, visibility: "Hidden"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Visibility", result.Error.Message, StringComparison.Ordinal);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_ToStricterUnitWithExistingNutritionAboveLimit_ReturnsValidationFailure() {
        var user = User.Create("update-product-stricter-unit@example.com", "hash");
        var product = Product.Create(
            user.Id,
            name: "Large piece",
            baseUnit: MeasurementUnit.Pcs,
            baseAmount: 1,
            defaultPortionAmount: 1,
            caloriesPerBase: Product.MaxWeightOrVolumeCaloriesPerBase + 1,
            proteinsPerBase: 0,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        var repository = new SingleProductRepository(product);
        var handler = new UpdateProductCommandHandler(
            repository,
            repository,
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, product.Id.Value, baseUnit: "g") with {
                BaseAmount = 100,
            },
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains(nameof(UpdateProductCommand.CaloriesPerBase), result.Error.Message, StringComparison.Ordinal);
        Assert.False(repository.UpdateCalled);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WithCurrentPieceUnitAndDefaultPortionAboveLimit_ReturnsValidationFailure() {
        var user = User.Create("update-product-piece-limit@example.com", "hash");
        var product = Product.Create(
            user.Id,
            name: "Vitamin",
            baseUnit: MeasurementUnit.Pcs,
            baseAmount: 1,
            defaultPortionAmount: 1,
            caloriesPerBase: 1,
            proteinsPerBase: 0,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        var repository = new SingleProductRepository(product);
        var handler = new UpdateProductCommandHandler(
            repository,
            repository,
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, product.Id.Value) with {
                DefaultPortionAmount = Product.MaxPieceDefaultPortionAmount + 1,
            },
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains(nameof(UpdateProductCommand.DefaultPortionAmount), result.Error.Message, StringComparison.Ordinal);
        Assert.False(repository.UpdateCalled);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WhenProductIsMissing_ReturnsNotAccessible() {
        var user = User.Create("update-product-missing@example.com", "hash");
        var productId = ProductId.New();
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, productId.Value, name: "Updated"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Product.NotAccessible", result.Error.Code);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WhenProductIsUsed_ReturnsValidationFailure() {
        var user = User.Create("update-product-used@example.com", "hash");
        var product = Product.Create(
            user.Id,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        SetProductUsageCollections(product, mealItemsCount: 0, recipeIngredientsCount: 1);
        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService();
        var handler = new UpdateProductCommandHandler(
            repository,
            repository,
            cleanup,
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, product.Id.Value, name: "Updated"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.UpdateCalled);
        Assert.Empty(cleanup.RequestedAssetIds);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WhenImageAssetAccessFails_ReturnsFailure() {
        var user = User.Create("update-product-forbidden-image@example.com", "hash");
        RecordingImageAssetAccessService access = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.Forbidden());
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(user),
            access);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, ProductId.New().Value, imageAssetId: Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }


    [Fact]
    public async Task UpdateProductCommandHandler_WithoutChanges_DoesNotPersistOrCleanup() {
        var user = User.Create("update-product@example.com", "hash");
        var product = Product.Create(
            user.Id,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            visibility: Visibility.Private);

        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService();
        var handler = new UpdateProductCommandHandler(repository, repository, cleanup, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new UpdateProductCommand(
                user.Id.Value,
                product.Id.Value,
                Barcode: null,
                ClearBarcode: false,
                Name: null,
                Brand: null,
                ClearBrand: false,
                ProductType: null,
                Category: null,
                ClearCategory: false,
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                BaseUnit: null,
                BaseAmount: null,
                DefaultPortionAmount: null,
                CaloriesPerBase: null,
                ProteinsPerBase: null,
                FatsPerBase: null,
                CarbsPerBase: null,
                FiberPerBase: null,
                AlcoholPerBase: null,
                Visibility: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.GetByIdForUpdateCalled);
        Assert.False(repository.UpdateCalled);
        Assert.Empty(cleanup.RequestedAssetIds);
    }

}
